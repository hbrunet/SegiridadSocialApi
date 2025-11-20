# ?? FlowId y Gestión de Sesiones Oracle

## ?? Problema

Oracle usa **Global Temporary Tables (GTT)** que son **específicas de la sesión**. 

Cuando se carga un archivo:
1. Se crea una sesión Oracle
2. Se carga el archivo en `USUARIO.TMP_NOV_DDJJ_PREV` (GTT)
3. **Si se cierra la sesión, los datos desaparecen**

## ? Solución: FlowId

El `FlowId` es un identificador que mantiene **la misma sesión Oracle** entre múltiples requests HTTP.

```
???????????????????????????????????????????????????????????
?  Request 1: Upload             ?
?  ?? Crea FlowId: "abc123"            ?
?  ?? Abre sesión Oracle   ?
?  ?? Carga datos en GTT ?????????????  ?
???????????????????????????????????????????????????????????
        ? SESIÓN SE MANTIENE
???????????????????????????????????????????????????????????
?  Request 2: Validar (con FlowId)   ?         ?
?  ?? Recibe FlowId: "abc123"        ?       ?
?  ?? Reutiliza misma sesión ?????????             ?
?  ?? Lee datos de GTT ?         ?
???????????????????????????????????????????????????????????
 ? SESIÓN SE MANTIENE
???????????????????????????????????????????????????????????
?  Request 3: Crear Hoja (con FlowId)? ?
?  ?? Recibe FlowId: "abc123"     ?   ?
?  ?? Reutiliza misma sesión ?????????    ?
?  ?? Lee datos de GTT ?         ?
?  ?? Cierra sesión al finalizar           ?
???????????????????????????????????????????????????????????
```

---

## ?? Uso en Validaciones

### Escenario 1: Auto-validación en Upload (Recomendado)

```http
POST /api/novedades/upload
Content-Type: multipart/form-data

file: archivo.txt
tipoNovedad: 1
autoValidar: true  ? ? NUEVO
```

**Respuesta:**
```json
{
  "file_name": "archivo.txt",
  "cantidad_registros": 1000,
  "id_archivo": 12345,
  "flow_id": "abc123",
  "validaciones": {
    "registros_validos": 950,
    "registros_errores": 47,
    "registros_advertencias": 3,
    "errores": [
      {
        "mensaje": "[CUIL_DUPLICADO] CUIL 20123456789 está duplicado",
        "linea": 15,
        "columna": 1
      }
    ]
  }
}
```

? **Ventajas:**
- Una sola llamada
- Validación inmediata
- FlowId disponible para siguiente paso

---

### Escenario 2: Validación Manual (Paso Separado)

**Paso 1: Upload**
```http
POST /api/novedades/upload

file: archivo.txt
tipoNovedad: 1
autoValidar: false
```

**Respuesta:**
```json
{
  "id_archivo": 12345,
  "flow_id": "abc123",  ? Guardar este valor
  "cantidad_registros": 1000
}
```

**Paso 2: Validar usando FlowId**
```http
POST /api/novedades/validar-archivo/12345?flowId=abc123
        ?
  Pasar el FlowId
```

**Respuesta:**
```json
{
  "registros_validos": 950,
  "registros_errores": 47,
  "errores": [...]
}
```

? **Ventajas:**
- Control fino del flujo
- Puedes decidir cuándo validar

---

### Escenario 3: Crear Hoja sin Validar

```http
POST /api/novedades/crear-hoja
Content-Type: application/json

{
  "idArchivo": 12345,
  "flowId": "abc123",  ? Requerido si hay GTT activa
  "tipoNovedad": 1,
  "grupoAdicional": 0,
  "tipoLiquidacion": 1,
  "cantidadRegistros": 1000,
  "periodo": "2024-01-01",
  "idRep": 5
}
```

---

## ?? Importante: FlowId es Obligatorio

### ? Sin FlowId (ERROR)

```http
POST /api/novedades/validar-archivo/12345
```

```json
{
  "error": "No se encontraron registros en la tabla temporal. 
  Verifique que el archivo fue cargado correctamente."
}
```

**Por qué falla:**
- Nueva sesión Oracle
- GTT vacía (datos estaban en otra sesión)

### ? Con FlowId (CORRECTO)

```http
POST /api/novedades/validar-archivo/12345?flowId=abc123
```

```json
{
  "registros_validos": 950,
  "errores": [...]
}
```

**Por qué funciona:**
- Misma sesión Oracle
- GTT contiene los datos

---

## ?? Seguridad y Expiración

### Expiración Automática

Los FlowIds expiran automáticamente:
- **Tiempo:** Configurable en `FlowSessionManager`
- **Por defecto:** 30 minutos de inactividad

### Validación de FlowId

```csharp
var conn = _flowSessionManager.GetConnection(flowId);
if (conn == null)
{
    throw new ApplicationException("El flujo expiró. Vuelva a cargar el archivo.");
}
```

---

## ?? Flujo Completo Recomendado

### Frontend (React/Angular/Vue)

```javascript
// 1. Upload con auto-validación
const formData = new FormData();
formData.append('file', file);
formData.append('tipoNovedad', 1);
formData.append('autoValidar', true);  // ? Auto-validar

const uploadResponse = await fetch('/api/novedades/upload', {
  method: 'POST',
  body: formData
});

const data = await uploadResponse.json();

// 2. Verificar validaciones
if (data.validaciones) {
  if (data.validaciones.registros_errores > 0) {
    console.error('Errores encontrados:', data.validaciones.errores);
    // Mostrar errores al usuario
    return;
  }
}

// 3. Crear hoja usando el FlowId
const crearHojaResponse = await fetch('/api/novedades/crear-hoja', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    idArchivo: data.id_archivo,
    flowId: data.flow_id,  // ? Usar FlowId del upload
 tipoNovedad: 1,
    grupoAdicional: 0,
    tipoLiquidacion: 1,
    cantidadRegistros: data.cantidad_registros,
    periodo: '2024-01-01',
    idRep: 5
})
});

// El FlowId se cierra automáticamente después de crear la hoja
```

---

## ?? Testing

### Curl - Upload con auto-validación

```bash
curl -X POST http://localhost:5000/api/novedades/upload \
  -F "file=@archivo.txt" \
  -F "tipoNovedad=1" \
  -F "autoValidar=true"
```

### Curl - Validar con FlowId

```bash
FLOW_ID="abc123"
ID_ARCHIVO=12345

curl -X POST "http://localhost:5000/api/novedades/validar-archivo/${ID_ARCHIVO}?flowId=${FLOW_ID}"
```

### Curl - Crear Hoja con FlowId

```bash
curl -X POST http://localhost:5000/api/novedades/crear-hoja \
  -H "Content-Type: application/json" \
  -d '{
    "idArchivo": 12345,
    "flowId": "abc123",
    "tipoNovedad": 1,
    "grupoAdicional": 0,
    "tipoLiquidacion": 1,
    "cantidadRegistros": 1000,
    "periodo": "2024-01-01T00:00:00",
    "idRep": 5
  }'
```

---

## ?? Mejores Prácticas

### ? Hacer

1. **Siempre usar `autoValidar=true`** en upload (más eficiente)
2. **Guardar el FlowId** para requests subsiguientes
3. **Validar antes de crear hoja** para evitar datos incorrectos
4. **Manejar expiración** del FlowId en el frontend

### ? Evitar

1. **No reutilizar FlowId** entre diferentes archivos
2. **No esperar mucho tiempo** entre upload y creación (puede expirar)
3. **No ignorar errores de validación**
4. **No llamar validación sin FlowId** después de upload

---

## ?? Comparación de Enfoques

| Enfoque | Requests | Eficiencia | Complejidad |
|---------|----------|------------|-------------|
| **Auto-validación** | 2 (upload + crear) | ??? Alta | ? Baja |
| **Validación manual** | 3 (upload + validar + crear) | ?? Media | ?? Media |
| **Sin validación** | 2 (upload + crear) | ? Baja (riesgoso) | ? Baja |

**Recomendado:** Auto-validación para UX óptima

---

## ?? Debugging

### Ver FlowId activos

```csharp
// En FlowSessionManager (agregar método temporal)
public int GetActiveFlowsCount()
{
    return _activeSessions.Count;
}
```

### Log de FlowId

```csharp
Log.Information("FlowId {FlowId} creado para archivo {IdArchivo}", flowId, idArchivo);
Log.Information("FlowId {FlowId} cerrado", flowId);
```

---

## ?? Resumen

| Concepto | Explicación |
|----------|-------------|
| **FlowId** | Identificador que mantiene sesión Oracle viva |
| **GTT** | Tabla temporal específica de sesión |
| **Auto-validación** | Validar automáticamente en upload |
| **Expiración** | FlowIds expiran tras inactividad |
| **Uso obligatorio** | Requerido para validaciones post-upload |

**?? Implementación lista para producción con gestión completa de sesiones Oracle!**
