# ?? Troubleshooting - Sistema de Validaciones

## ?? Problemas Comunes y Soluciones

### 1. "No se encontraron registros en la tabla temporal"

**Error completo:**
```json
{
  "error": "No se encontraron registros en la tabla temporal. Verifique que el archivo fue cargado correctamente."
}
```

**Causa:** Validación sin FlowId o FlowId expirado

**Solución:**
```javascript
// ? Incorrecto
const response1 = await fetch('/api/novedades/upload', { ... });
const response2 = await fetch(`/api/novedades/validar-archivo/${id}`); // Sin FlowId

// ? Correcto
const response1 = await fetch('/api/novedades/upload', { ... });
const data = await response1.json();
const response2 = await fetch(`/api/novedades/validar-archivo/${id}?flowId=${data.flow_id}`);
```

---

### 2. "El flujo expiró o es inválido"

**Error completo:**
```json
{
  "error": "El flujo 'abc123' expiró o es inválido. La tabla temporal no está disponible."
}
```

**Causa:** FlowId expiró (> 30 minutos de inactividad)

**Solución:**
1. Volver a cargar el archivo
2. Reducir tiempo entre upload y validación
3. Usar `autoValidar=true` para validar inmediatamente

```javascript
// ? Mejor opción: Auto-validación
const formData = new FormData();
formData.append('file', file);
formData.append('tipoNovedad', 1);
formData.append('autoValidar', true); // Valida inmediatamente

const response = await fetch('/api/novedades/upload', {
  method: 'POST',
  body: formData
});
```

---

### 3. Validaciones retornan 0 registros

**Síntoma:** 
```json
{
  "registros_validos": 0,
  "registros_errores": 0,
  "registros_advertencias": 0
}
```

**Causas posibles:**

#### A) No se usó FlowId
```javascript
// ? Sin FlowId
await fetch('/api/novedades/validar-archivo/123');

// ? Con FlowId
await fetch('/api/novedades/validar-archivo/123?flowId=abc123');
```

#### B) GTT realmente vacía
```sql
-- Verificar en Oracle
SELECT COUNT(*) FROM USUARIO.TMP_NOV_DDJJ_PREV;
```

Si retorna 0:
- Revisar procedimiento `CREAR_EXTAB_ARCHIVO`
- Verificar permisos de lectura del archivo FTP
- Revisar logs de Oracle

---

### 4. "Error al ejecutar regla 'XXX'"

**Síntoma:**
```json
{
  "errores": [
    {
      "mensaje": "[CUIL_DUPLICADO] Error al ejecutar regla 'CUIL_DUPLICADO': ORA-00942: table or view does not exist",
      "linea": 0,
      "columna": 0
    }
  ]
}
```

**Causa:** Query SQL en regla con error

**Solución:**
1. Verificar que la tabla existe en el schema correcto
2. Revisar permisos del usuario Oracle
3. Probar query directamente en Oracle

```sql
-- Probar query de la regla
SELECT CUIL, COUNT(*) AS Cantidad 
FROM USUARIO.TMP_NOV_DDJJ_PREV
GROUP BY CUIL
HAVING COUNT(*) > 1;
```

---

### 5. Errores de Timeout en Validaciones

**Síntoma:**
```
System.TimeoutException: The operation has timed out.
```

**Causa:** Archivo muy grande o queries lentas

**Solución:**

#### A) Aumentar timeout de conexión
```json
// appsettings.json
{
  "ConnectionStrings": {
    "Oracle": "User Id=...; Password=...; Data Source=...; Connection Timeout=120;"
      ? Aumentar a 120 segundos
  }
}
```

#### B) Optimizar queries en reglas
```csharp
// ? Query lenta
SELECT * FROM USUARIO.TMP_NOV_DDJJ_PREV WHERE ...

// ? Query optimizada (solo columnas necesarias)
SELECT ROWNUM, CUIL, PERIODO FROM USUARIO.TMP_NOV_DDJJ_PREV WHERE ...
```

#### C) Agregar índices a GTT (si Oracle lo permite)
```sql
CREATE INDEX idx_tmp_cuil ON USUARIO.TMP_NOV_DDJJ_PREV(CUIL);
```

---

### 6. "The name 'Log' does not exist in the current context"

**Error en compilación:**
```
CS0103: The name 'Log' does not exist in the current context
```

**Solución:**
```csharp
// Agregar using
using Serilog;

// O usar ILogger inyectado
private readonly ILogger<NovedadesService> _logger;

public NovedadesService(ILogger<NovedadesService> logger, ...)
{
    _logger = logger;
}

// Usar en código
_logger.LogWarning("Error al validar archivo {IdArchivo}", idArchivo);
```

---

### 7. FlowId no se mantiene entre requests

**Síntoma:** Cada request tiene un FlowId diferente

**Causa:** No se está guardando el FlowId en el cliente

**Solución:**

```javascript
// ? Guardar FlowId en estado
const [flowId, setFlowId] = useState(null);

// Upload
const uploadResponse = await fetch('/api/novedades/upload', ...);
const uploadData = await uploadResponse.json();
setFlowId(uploadData.flow_id); // Guardar

// Validar (usando el mismo FlowId)
const validarResponse = await fetch(
  `/api/novedades/validar-archivo/${uploadData.id_archivo}?flowId=${flowId}`
);

// Crear hoja (usando el mismo FlowId)
await fetch('/api/novedades/crear-hoja', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    idArchivo: uploadData.id_archivo,
    flowId: flowId,  // Mismo FlowId
    ...
  })
});
```

---

### 8. Validaciones pasan pero crear hoja falla

**Síntoma:**
```
Validaciones OK ? Crear hoja ERROR
```

**Causa:** FlowId se cerró prematuramente

**Solución:**

```csharp
// ? Incorrecto: Cerrar FlowId en validación
public async Task<ValidacionArchivoDto> ValidarArchivo(...)
{
    var resultado = await _executor.EjecutarValidacionesAsync(...);
    await _flowSessionManager.EndAsync(flowId); // ? NO hacer esto
    return resultado;
}

// ? Correcto: Solo cerrar en crear hoja
public async Task<CrearHojaResponse> CrearHoja(...)
{
    var nroHoja = await _hojaRepository.CrearHojaAsync(...);
    await _flowSessionManager.EndAsync(flowId); // ? Aquí sí
    return response;
}
```

---

### 9. Regla no se ejecuta

**Síntoma:** Una regla específica no aparece en resultados

**Solución:**

#### A) Verificar registro en DI
```csharp
// Startup.cs
services.AddScoped<IValidacionRule, MiNuevaRegla>(); // ¿Está registrada?
```

#### B) Verificar orden de ejecución
```csharp
public override int Orden => 999; // Si es muy alto, puede no ejecutarse si hay error previo
```

#### C) Verificar tipo
```csharp
public override TipoValidacion Tipo => TipoValidacion.Error; // ¿Error o Advertencia?
```

#### D) Debugging
```csharp
protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(...)
{
    Console.WriteLine($"Ejecutando regla: {NombreRegla}"); // Log temporal
    var sql = "...";
    var resultados = await connection.QueryAsync(...);
    Console.WriteLine($"Encontrados {resultados.Count()} errores"); // Log temporal
    return resultados.Select(...).ToList();
}
```

---

### 10. Memoria alta / Leaks

**Síntoma:** Uso de memoria aumenta con cada upload

**Causa:** FlowIds no se cierran correctamente

**Solución:**

#### A) Verificar cierre en finally
```csharp
try
{
    flowId = await _flowSessionManager.StartAsync();
    // ... operaciones ...
}
finally
{
    if (!string.IsNullOrEmpty(flowId))
    {
    await _flowSessionManager.EndAsync(flowId);
    }
}
```

#### B) Habilitar auto-limpieza
```csharp
// FlowSessionManager
private readonly Timer _cleanupTimer;

public FlowSessionManager()
{
    // Limpiar FlowIds expirados cada 10 minutos
    _cleanupTimer = new Timer(CleanupExpiredSessions, null, 
        TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
}

private void CleanupExpiredSessions(object? state)
{
    var expired = _activeSessions
        .Where(s => DateTime.UtcNow - s.Value.LastAccess > TimeSpan.FromMinutes(30))
        .ToList();
    
    foreach (var session in expired)
    {
   _activeSessions.TryRemove(session.Key, out _);
        session.Value.Connection.Dispose();
    }
}
```

---

## ?? Herramientas de Debugging

### 1. Endpoint de Health Check

```csharp
[HttpGet("validaciones/health")]
public IActionResult HealthCheck()
{
    return Ok(new
  {
        activeFlows = _flowSessionManager.GetActiveFlowsCount(),
        registeredRules = _validacionExecutor.ObtenerReglasRegistradas().Count,
        status = "healthy"
    });
}
```

### 2. Logs detallados

```csharp
// En ValidacionExecutor
Log.Information("Iniciando validaciones para archivo {IdArchivo} con {CantReglas} reglas",
    idArchivo, _reglas.Count);

foreach (var regla in _reglas)
{
    var stopwatch = Stopwatch.StartNew();
    var detalles = await regla.ValidarAsync(connection, idArchivo);
    stopwatch.Stop();
 
    Log.Information("Regla {Regla} completada en {Ms}ms con {Errores} errores",
   regla.NombreRegla, stopwatch.ElapsedMilliseconds, detalles.Count);
}
```

### 3. Query de diagnóstico Oracle

```sql
-- Ver sesiones activas con GTT
SELECT s.sid, s.serial#, s.username, s.program, s.logon_time,
       (SELECT COUNT(*) FROM USUARIO.TMP_NOV_DDJJ_PREV) AS registros_gtt
FROM v$session s
WHERE s.username = 'USUARIO'
  AND s.status = 'ACTIVE';
```

---

## ?? Checklist de Troubleshooting

- [ ] ¿Se está usando FlowId en validaciones?
- [ ] ¿El FlowId no expiró (< 30 minutos)?
- [ ] ¿La regla está registrada en Startup.cs?
- [ ] ¿El query SQL de la regla es válido?
- [ ] ¿La tabla GTT tiene datos?
- [ ] ¿Los logs muestran errores?
- [ ] ¿El FlowId se cierra correctamente?
- [ ] ¿Hay timeouts de conexión?

---

## ?? Soporte

Si el problema persiste:

1. **Revisar logs**: `logs/log-YYYY-MM-DD.txt`
2. **Verificar Oracle**: Conectarse y revisar GTT manualmente
3. **Test endpoints**: Usar Postman/Insomnia
4. **Consultar docs**: Ver [FLOWID.md](FLOWID.md) y [ARQUITECTURA.md](ARQUITECTURA.md)

---

**?? Tip:** La mayoría de problemas se resuelven usando `autoValidar=true` en el upload
