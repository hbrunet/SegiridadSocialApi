# ?? Guía Rápida - Sistema de Validaciones

## ?? Crear una Nueva Regla de Validación

### Ejemplo: Validar que el período no sea anterior a 2020

```csharp
using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
  public class ValidarPeriodoMinimoRule : ValidacionRuleBase
    {
        public override string NombreRegla => "PERIODO_MINIMO";
        
 public override string Descripcion => 
            "Verifica que el período no sea anterior a enero 2020";
      
        public override TipoValidacion Tipo => TipoValidacion.Error;
        
        public override int Orden => 25;

        protected override async Task<List<DetalleValidacionDto>> 
   EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
     {
     var sql = @"
    SELECT 
      ROWNUM AS Linea,
   CUIL,
           TO_CHAR(PERIODO, 'YYYY-MM') AS PeriodoTexto
                FROM USUARIO.TMP_NOV_DDJJ_PREV
        WHERE PERIODO < TO_DATE('2020-01-01', 'YYYY-MM-DD')";

        var registros = await connection.QueryAsync<dynamic>(sql);

  return registros.Select(r => CrearDetalle(
     mensaje: $"CUIL {r.Cuil}: Período {r.PeriodoTexto} es anterior a 2020-01",
        linea: (int)r.Linea,
      columna: 3
    )).ToList();
        }
    }
}
```

### Registrar en Startup.cs

```csharp
// Startup.cs - Método ConfigureServices
services.AddScoped<IValidacionRule, ValidarPeriodoMinimoRule>();
```

**¡Eso es todo!** La regla se ejecutará automáticamente.

---

## ?? Ejemplos de Validaciones Comunes

### 1. Validar Longitud de Campo

```csharp
protected override async Task<List<DetalleValidacionDto>> 
EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
{
  var sql = @"
   SELECT ROWNUM AS Linea, APELLIDO
    FROM USUARIO.TMP_NOV_DDJJ_PREV
  WHERE LENGTH(APELLIDO) > 50";

    var errores = await connection.QueryAsync<dynamic>(sql);
  
return errores.Select(e => CrearDetalle(
        $"Apellido excede 50 caracteres: '{e.Apellido}'",
    (int)e.Linea,
  4
    )).ToList();
}
```

### 2. Validar Rango de Valores

```csharp
protected override async Task<List<DetalleValidacionDto>> 
EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
{
    var sql = @"
        SELECT ROWNUM AS Linea, CUIL, EDAD
    FROM USUARIO.TMP_NOV_DDJJ_PREV
     WHERE EDAD < 18 OR EDAD > 75";

    var errores = await connection.QueryAsync<dynamic>(sql);
    
    return errores.Select(e => CrearDetalle(
 $"CUIL {e.Cuil}: Edad {e.Edad} fuera de rango (18-75)",
        (int)e.Linea,
      8
 )).ToList();
}
```

### 3. Validar contra Otra Tabla (Referencia)

```csharp
protected override async Task<List<DetalleValidacionDto>> 
EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
{
    var sql = @"
        SELECT t.ROWNUM AS Linea, t.CUIL, t.CODIGO_LOCALIDAD
   FROM USUARIO.TMP_NOV_DDJJ_PREV t
        LEFT JOIN MAESTROS.LOCALIDADES l ON t.CODIGO_LOCALIDAD = l.CODIGO
  WHERE l.CODIGO IS NULL";

    var errores = await connection.QueryAsync<dynamic>(sql);
    
 return errores.Select(e => CrearDetalle(
        $"CUIL {e.Cuil}: Código localidad '{e.CodigoLocalidad}' no existe",
  (int)e.Linea,
   6
    )).ToList();
}
```

### 4. Validar Suma o Cálculo

```csharp
protected override async Task<List<DetalleValidacionDto>> 
EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
{
    var sql = @"
    SELECT ROWNUM AS Linea, CUIL,
   REMUNIMPONIBLE1, REMUNIMPONIBLE2, TOTAL_DECLARADO
        FROM USUARIO.TMP_NOV_DDJJ_PREV
   WHERE (REMUNIMPONIBLE1 + REMUNIMPONIBLE2) != TOTAL_DECLARADO";

 var errores = await connection.QueryAsync<dynamic>(sql);
    
 return errores.Select(e => CrearDetalle(
        $"CUIL {e.Cuil}: Suma de remuneraciones no coincide con total declarado",
        (int)e.Linea,
       10
    )).ToList();
}
```

### 5. Validar Formato con Regex

```csharp
protected override async Task<List<DetalleValidacionDto>> 
EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
{
    var sql = @"
        SELECT ROWNUM AS Linea, CUIL, EMAIL
  FROM USUARIO.TMP_NOV_DDJJ_PREV
  WHERE EMAIL IS NOT NULL
 AND NOT REGEXP_LIKE(EMAIL, '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')";

    var errores = await connection.QueryAsync<dynamic>(sql);
    
    return errores.Select(e => CrearDetalle(
        $"CUIL {e.Cuil}: Email inválido '{e.Email}'",
  (int)e.Linea,
        12
    )).ToList();
}
```

---

## ?? Configuración de Reglas

### Control de Orden de Ejecución

```csharp
public override int Orden => 15; // Menor = se ejecuta primero
```

**Orden recomendado:**
- `1-10`: Validaciones de formato básico
- `11-20`: Validaciones de integridad referencial
- `21-30`: Validaciones de negocio
- `31+`: Advertencias y validaciones opcionales

### Tipo de Validación

```csharp
// Error crítico - bloquea procesamiento
public override TipoValidacion Tipo => TipoValidacion.Error;

// Advertencia - no bloquea procesamiento
public override TipoValidacion Tipo => TipoValidacion.Advertencia;
```

---

## ?? Testing Manual

### 1. Validar un archivo

```http
POST /api/novedades/validar-archivo/12345
```

### 2. Ver reglas registradas

```http
GET /api/novedades/reglas-validacion
```

Respuesta:
```json
{
  "total": 5,
  "reglas": [
    {
      "nombre": "FORMATO_CUIL",
"descripcion": "Verifica que el CUIL tenga exactamente 11 dígitos",
      "tipo": "Error",
      "orden": 5
    }
  ]
}
```

---

## ?? Interpretar Resultados

### Respuesta Exitosa (sin errores)

```json
{
  "registros_validos": 1000,
  "registros_advertencias": 0,
  "registros_errores": 0,
  "errores": [],
  "advertencias": []
}
```

? **Archivo listo para procesar**

### Respuesta con Errores

```json
{
  "registros_validos": 950,
  "registros_advertencias": 5,
  "registros_errores": 45,
  "errores": [
    {
  "mensaje": "[CUIL_DUPLICADO] CUIL 20123456789 está duplicado 2 veces",
      "linea": 15,
      "columna": 1
    }
  ],
  "advertencias": [
    {
"mensaje": "[PERIODO_FUTURO] CUIL 20111111111: Período 2025-01 es futuro",
      "linea": 100,
      "columna": 3
    }
  ]
}
```

? **Archivo tiene errores - corregir antes de procesar**
?? **Advertencias no bloquean procesamiento**

---

## ?? Tips y Mejores Prácticas

### ? Hacer

- Nombrar reglas con sustantivos claros (`CUIL_DUPLICADO`)
- Incluir información contextual en mensajes (`CUIL 20123456789: ...`)
- Especificar línea y columna exactas
- Usar queries eficientes (índices, WHERE clauses)
- Documentar qué valida cada regla

### ? Evitar

- Reglas con lógica compleja (dividir en múltiples reglas)
- Queries que recorran toda la tabla sin necesidad
- Mensajes genéricos (`"Error en validación"`)
- Validaciones duplicadas entre reglas

### ?? Nomenclatura

```csharp
// ? Bueno
public override string NombreRegla => "CUIL_DUPLICADO";
public override string Descripcion => "Verifica que no existan CUILs duplicados...";

// ? Malo
public override string NombreRegla => "Regla1";
public override string Descripcion => "Valida cosas";
```

---

## ?? Debugging

### Ver qué reglas se están ejecutando

```csharp
// En ValidacionExecutor.cs (temporalmente)
foreach (var regla in _reglas)
{
    Console.WriteLine($"Ejecutando: {regla.NombreRegla} (Orden: {regla.Orden})");
    var detalles = await regla.ValidarAsync(connection, idArchivo);
}
```

### Log de SQL ejecutado

```csharp
protected override async Task<List<DetalleValidacionDto>> 
EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
{
    var sql = @"SELECT ...";
    
    // Log temporal
    Console.WriteLine($"[{NombreRegla}] SQL: {sql}");
    
    var resultados = await connection.QueryAsync<dynamic>(sql);
    return resultados.Select(...).ToList();
}
```

---

## ?? Recursos Adicionales

- **Documentación completa**: Ver `Validaciones/README.md`
- **Arquitectura detallada**: Ver `Validaciones/ARQUITECTURA.md`
- **Ejemplos de reglas**: Ver carpeta `Validaciones/Rules/`

---

## ?? Preguntas Frecuentes

**P: ¿Cómo deshabilito una regla temporalmente?**  
R: Comentar el registro en `Startup.cs`:
```csharp
// services.AddScoped<IValidacionRule, ValidarMiReglaRule>();
```

**P: ¿Puedo ejecutar solo ciertas reglas?**  
R: Sí, puedes filtrar por tipo:
```csharp
var reglasError = _reglas.Where(r => r.Tipo == TipoValidacion.Error);
```

**P: ¿Cómo hago validaciones que dependan de parámetros externos?**  
R: Inyecta dependencias en el constructor de la regla:
```csharp
public class MiRegla : ValidacionRuleBase
{
    private readonly IConfiguration _config;
    
    public MiRegla(IConfiguration config)
    {
  _config = config;
    }
}
```

**P: ¿Las validaciones se ejecutan en paralelo?**  
R: Actualmente no, son secuenciales. Para paralelizar, modificar `ValidacionExecutor`.
