# ?? Catálogo Completo de Reglas de Validación

## ?? Resumen Ejecutivo

**Total de reglas:** 11  
**Reglas de Error:** 8  
**Reglas de Advertencia:** 3  
**Cobertura:** 100% funcional

---

## ?? Reglas de Error (Críticas)

### 1. FORMATO_CUIL
**Orden:** 5  
**Archivo:** `ValidarFormatoCuilRule.cs`

**Descripción:** Verifica que el CUIL tenga exactamente 11 dígitos numéricos

**Validaciones:**
- ? CUIL no nulo
- ? Longitud exacta de 11 caracteres
- ? Solo dígitos numéricos (sin letras ni caracteres especiales)

**Query SQL:**
```sql
SELECT ROWNUM AS Linea, CUIL
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CUIL IS NULL
   OR LENGTH(CUIL) != 11
 OR REGEXP_LIKE(CUIL, '[^0-9]');
```

**Ejemplo de error:**
```json
{
  "mensaje": "[FORMATO_CUIL] CUIL inválido: '2012345' (debe tener 11 dígitos numéricos)",
  "linea": 5,
  "columna": 1
}
```

---

### 2. CAMPOS_OBLIGATORIOS_GTT
**Orden:** 8  
**Archivo:** `ValidarCamposObligatoriosGttRule.cs`

**Descripción:** Verifica que los campos obligatorios tengan valores válidos

**Validaciones:**
- ? CUIL no nulo
- ? APENOM no vacío
- ? Al menos una REMUNIMPONIBLE* > 0
- ? CODSITUACION no nulo

**Queries SQL:**
```sql
-- CUIL nulo
SELECT ROWNUM AS Linea, CUIL
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CUIL IS NULL;

-- APENOM vacío
SELECT ROWNUM AS Linea, CUIL
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE APENOM IS NULL OR TRIM(APENOM) = '';

-- Sin remuneraciones
SELECT ROWNUM AS Linea, CUIL
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE (REMUNIMPONIBLE1 IS NULL OR REMUNIMPONIBLE1 = 0)
  AND (REMUNIMPONIBLE2 IS NULL OR REMUNIMPONIBLE2 = 0)
  AND ... -- Todas las REMUNIMPONIBLE
```

**Ejemplos de error:**
```json
[
  {
 "mensaje": "[CAMPOS_OBLIGATORIOS_GTT] CUIL es obligatorio y no puede ser nulo",
    "linea": 10,
    "columna": 2
  },
  {
    "mensaje": "[CAMPOS_OBLIGATORIOS_GTT] CUIL 20123456789: APENOM (Apellido y Nombre) es obligatorio",
    "linea": 15,
    "columna": 3
  },
  {
    "mensaje": "[CAMPOS_OBLIGATORIOS_GTT] CUIL 20987654321: Debe tener al menos una remuneración imponible con valor > 0",
    "linea": 42,
    "columna": 15
  }
]
```

---

### 3. CUIL_DUPLICADO
**Orden:** 10  
**Archivo:** `ValidarCuilDuplicadoRule.cs`

**Descripción:** Detecta CUILs duplicados dentro del mismo archivo

**Validaciones:**
- ? CUILs únicos en el archivo

**Query SQL:**
```sql
SELECT CUIL, COUNT(*) AS Cantidad, MIN(ROWNUM) AS PrimeraLinea
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CUIL IS NOT NULL
GROUP BY CUIL
HAVING COUNT(*) > 1
ORDER BY CUIL;
```

**Ejemplo de error:**
```json
{
  "mensaje": "[CUIL_DUPLICADO] CUIL 20123456789 está duplicado 2 veces en el archivo",
  "linea": 15,
  "columna": 1
}
```

---

### 4. CODIGO_ACTIVIDAD_VALIDO
**Orden:** 15  
**Archivo:** `ValidarCodigoActividadRule.cs`

**Descripción:** Verifica que CODACTIVIDAD tenga uno de los valores permitidos

**Valores permitidos:** `19, 32, 45, 52, 65, 76, 77, 85, 913`

**Validaciones:**
- ? CODACTIVIDAD no nulo
- ? Valor dentro de la lista permitida

**Queries SQL:**
```sql
-- Nulos
SELECT ROWNUM AS Linea, CUIL, CODACTIVIDAD
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CODACTIVIDAD IS NULL;

-- Valores inválidos
SELECT ROWNUM AS Linea, CUIL, CODACTIVIDAD
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CODACTIVIDAD IS NOT NULL
  AND CODACTIVIDAD NOT IN (19, 32, 45, 52, 65, 76, 77, 85, 913);
```

**Ejemplos de error:**
```json
[
  {
    "mensaje": "[CODIGO_ACTIVIDAD_VALIDO] CUIL 20123456789: CODACTIVIDAD es obligatorio y no puede ser nulo",
    "linea": 8,
    "columna": 8
  },
  {
"mensaje": "[CODIGO_ACTIVIDAD_VALIDO] CUIL 20987654321: CODACTIVIDAD = 99 no es válido (valores permitidos: 19, 32, 45, 52, 65, 76, 77, 85, 913)",
    "linea": 25,
    "columna": 8
  }
]
```

---

### 5. TIPO_EMPRESA_VALIDO
**Orden:** 16  
**Archivo:** `ValidarTipoEmpresaRule.cs`

**Descripción:** Verifica que TIPOEMPRESA tenga uno de los valores permitidos

**Valores permitidos:** `'3'`, `'G'`

**Validaciones:**
- ? TIPOEMPRESA no nulo
- ? Valor '3' o 'G'

**Queries SQL:**
```sql
-- Nulos
SELECT ROWNUM AS Linea, CUIL, TIPOEMPRESA
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE TIPOEMPRESA IS NULL;

-- Valores inválidos
SELECT ROWNUM AS Linea, CUIL, TIPOEMPRESA
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE TIPOEMPRESA IS NOT NULL
  AND TIPOEMPRESA NOT IN ('3', 'G');
```

**Ejemplos de error:**
```json
[
  {
    "mensaje": "[TIPO_EMPRESA_VALIDO] CUIL 20555666777: TIPOEMPRESA es obligatorio y no puede ser nulo",
  "linea": 12,
    "columna": 38
  },
  {
    "mensaje": "[TIPO_EMPRESA_VALIDO] CUIL 20123456789: TIPOEMPRESA = 'X' no es válido (valores permitidos: '3', 'G')",
    "linea": 30,
    "columna": 38
  }
]
```

---

### 6. CODIGO_CONDICION_VALIDO
**Orden:** 17  
**Archivo:** `ValidarCodigoCondicionRule.cs`

**Descripción:** Verifica que CODCONDICION tenga uno de los valores permitidos

**Valores permitidos:** `1, 2, 5`

**Validaciones:**
- ? CODCONDICION no nulo
- ? Valor dentro de la lista permitida

**Queries SQL:**
```sql
-- Nulos
SELECT ROWNUM AS Linea, CUIL, CODCONDICION
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CODCONDICION IS NULL;

-- Valores inválidos
SELECT ROWNUM AS Linea, CUIL, CODCONDICION
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CODCONDICION IS NOT NULL
  AND CODCONDICION NOT IN (1, 2, 5);
```

**Ejemplos de error:**
```json
{
    "mensaje": "[CODIGO_CONDICION_VALIDO] CUIL 20123456789: CODCONDICION es obligatorio y no puede ser nulo",
    "linea": 8,
    "columna": 7
  },
  {
    "mensaje": "[CODIGO_CONDICION_VALIDO] CUIL 20987654321: CODCONDICION = 3 no es válido (valores permitidos: 1, 2, 5)",
    "linea": 18,
    "columna": 7
  }
]
```

---

### 7. OBRA_SOCIAL_NACIONAL_REQUERIDA
**Orden:** 21  
**Archivo:** `ValidarObraSocialNacionalRule.cs`

**Descripción:** Verifica que para las actividades 46 y 77 exista importe de obra social nacional

**Actividades que requieren validación:** `46, 77`

**Validaciones:**
- ? Para CODACTIVIDAD 46: REMUNIMPONIBLE4 > 0
- ? Para CODACTIVIDAD 77: REMUNIMPONIBLE4 > 0

**Query SQL:**
```sql
SELECT ROWNUM AS Linea, 
       CUIL, 
       CODACTIVIDAD,
       NVL(REMUNIMPONIBLE4, 0) AS RemunImponible4
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CODACTIVIDAD IN (46, 77)
  AND (REMUNIMPONIBLE4 IS NULL OR REMUNIMPONIBLE4 <= 0);
```

**Ejemplo de error:**
```json
{
  "mensaje": "[OBRA_SOCIAL_NACIONAL_REQUERIDA] CUIL 20123456789: CODACTIVIDAD 46 requiere REMUNIMPONIBLE4 (Obra Social Nacional) con valor positivo (actual: 0)",
  "linea": 35,
  "columna": 24
}
```

**Explicación:**  
Las actividades 46 y 77 corresponden a trabajadores que deben estar afiliados obligatoriamente a una Obra Social Nacional, por lo que deben tener un aporte registrado en REMUNIMPONIBLE4.

---

### 8. REMUNERACION_POSITIVA
**Orden:** 20  
**Archivo:** `ValidarRemuneracionPositivaRule.cs`

**Descripción:** Verifica que todas las remuneraciones sean >= 0

**Validaciones:**
- ? REMUNIMPONIBLE1 >= 0
- ? REMUNIMPONIBLE2 >= 0
- ? REMUNIMPONIBLE3 >= 0
- ? (y todas las demás remuneraciones)

**Query SQL:**
```sql
SELECT ROWNUM AS Linea, CUIL,
       CASE 
  WHEN REMUNIMPONIBLE1 < 0 THEN 'REMUNIMPONIBLE1'
         WHEN REMUNIMPONIBLE2 < 0 THEN 'REMUNIMPONIBLE2'
         WHEN REMUNIMPONIBLE3 < 0 THEN 'REMUNIMPONIBLE3'
         -- ...
       END AS Columna,
  CASE 
         WHEN REMUNIMPONIBLE1 < 0 THEN REMUNIMPONIBLE1
         WHEN REMUNIMPONIBLE2 < 0 THEN REMUNIMPONIBLE2
         -- ...
       END AS ValorNegativo
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE REMUNIMPONIBLE1 < 0 
   OR REMUNIMPONIBLE2 < 0 
   OR REMUNIMPONIBLE3 < 0;
```

**Ejemplo de error:**
```json
{
  "mensaje": "[REMUNERACION_POSITIVA] CUIL 20111222333: REMUNIMPONIBLE1 tiene valor negativo (-1000)",
  "linea": 42,
  "columna": 5
}
```

---

### 9. ASIGNACION_FAMILIAR_VALIDA
**Orden:** 25  
**Archivo:** `ValidarAsignacionFamiliarRule.cs`

**Descripción:** Verifica que las asignaciones familiares sean coherentes

**Validaciones:**
- ? ASIGFAMPAGADAS >= 0
- ? Si ASIGFAMPAGADAS > 0, entonces CANTHIJOS > 0

**Queries SQL:**
```sql
-- Nulas o negativas
SELECT ROWNUM AS Linea, CUIL, ASIGFAMPAGADAS
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE ASIGFAMPAGADAS IS NULL OR ASIGFAMPAGADAS < 0;

-- Hijos cero con asignaciones
SELECT ROWNUM AS Linea, CUIL
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE ASIGFAMPAGADAS > 0 
  AND (CANTHIJOS IS NULL OR CANTHIJOS = 0);
```

**Ejemplos de error:**
```json
[
  {
    "mensaje": "[ASIGNACION_FAMILIAR_VALIDA] CUIL 20123456789: ASIGFAMPAGADAS no puede ser negativo",
    "linea": 5,
    "columna": 5
  },
  {
    "mensaje": "[ASIGNACION_FAMILIAR_VALIDA] CUIL 20333444555: Tiene asignaciones familiares pagadas pero 0 hijos declarados",
    "linea": 10,
    "columna": 5
  }
]
```

---

## ?? Reglas de Advertencia (No críticas)

### 10. RANGOS_CAMPOS
**Orden:** 30  
**Archivo:** `ValidarRangosCamposRule.cs`

**Descripción:** Verifica que los campos numéricos estén dentro de rangos válidos

**Validaciones:**
- ?? CANTHIJOS entre 0-20
- ?? CANTADHERENTES entre 0-10
- ?? CANT_DIAS_TRABA entre 0-31
- ?? CANTHORASEXTRA entre 0-200
- ?? HORASTRAB entre 0-400

**Queries SQL:**
```sql
-- Ejemplo: Hijos fuera de rango
SELECT ROWNUM AS Linea, CUIL, CANTHIJOS
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CANTHIJOS IS NOT NULL 
  AND (CANTHIJOS < 0 OR CANTHIJOS > 20);
```

**Ejemplos de advertencia:**
```json
[
  {
    "mensaje": "[RANGOS_CAMPOS] CUIL 20123456789: CANTHIJOS = 25 parece inusual (rango esperado: 0-20)",
    "linea": 5,
    "columna": 5
  },
  {
    "mensaje": "[RANGOS_CAMPOS] CUIL 20987654321: CANT_DIAS_TRABA = 35 es inválido (debe estar entre 0 y 31)",
    "linea": 78,
    "columna": 40
  }
]
```

---

### 11. CONSISTENCIA_CAMPOS
**Orden:** 35  
**Archivo:** `ValidarConsistenciaCamposRule.cs`

**Descripción:** Verifica consistencia lógica entre campos relacionados

**Validaciones:**
- ?? Cónyuge = 'S' pero CANTHIJOS = 0 (verificar)
- ?? ASIGFAMPAGADAS > 0 pero CANTHIJOS = 0
- ?? CANT_DIAS_TRABA = 0 pero tiene remuneraciones
- ?? HORAS_EXTRA > 0 pero CANTHORASEXTRA = 0

**Queries SQL:**
```sql
-- Ejemplo: Asignaciones familiares sin hijos
SELECT ROWNUM AS Linea, CUIL, ASIGFAMPAGADAS, CANTHIJOS
FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE ASIGFAMPAGADAS > 0 
  AND (CANTHIJOS IS NULL OR CANTHIJOS = 0);
```

**Ejemplos de advertencia:**
```json
[
  {
    "mensaje": "[CONSISTENCIA_CAMPOS] CUIL 20333444555: Tiene asignaciones familiares pagadas pero 0 hijos declarados",
    "linea": 42,
    "columna": 16
  },
  {
    "mensaje": "[CONSISTENCIA_CAMPOS] CUIL 20555666777: Tiene remuneraciones pero 0 días trabajados declarados",
 "linea": 100,
    "columna": 40
  }
]
```

---

## ?? Orden de Ejecución

```
1. FORMATO_CUIL (Orden: 5)
   ?
2. CAMPOS_OBLIGATORIOS_GTT (Orden: 8)
   ?
3. CUIL_DUPLICADO (Orden: 10)
   ?
4. CODIGO_ACTIVIDAD_VALIDO (Orden: 15)
   ?
5. TIPO_EMPRESA_VALIDO (Orden: 16)
   ?
6. CODIGO_CONDICION_VALIDO (Orden: 17)
   ?
7. REMUNERACION_POSITIVA (Orden: 20)
   ?
8. OBRA_SOCIAL_NACIONAL_REQUERIDA (Orden: 21)
   ?
9. RANGOS_CAMPOS (Orden: 25)
   ?
10. CONSISTENCIA_CAMPOS (Orden: 30)
```

---

## ?? Uso en API

### Listar todas las reglas

```http
GET /api/novedades/reglas-validacion
```

**Respuesta:**
```json
{
  "total": 11,
  "reglas": [
    {
      "nombre": "FORMATO_CUIL",
      "descripcion": "Verifica que el CUIL tenga exactamente 11 dígitos numéricos",
      "tipo": "Error",
      "orden": 5
    },
    {
      "nombre": "CAMPOS_OBLIGATORIOS_GTT",
      "descripcion": "Verifica que los campos obligatorios (CUIL, APENOM, Remuneraciones) tengan valores válidos",
      "tipo": "Error",
      "orden": 8
    },
    // ... resto de reglas
  ]
}
```

### Validar archivo

```http
POST /api/novedades/validar-archivo/12345?flowId=abc123
```

**Respuesta con errores y advertencias:**
```json
{
  "registros_validos": 950,
  "registros_errores": 47,
  "registros_advertencias": 15,
  "errores": [
    {
      "mensaje": "[FORMATO_CUIL] CUIL inválido: '123' (debe tener 11 dígitos numéricos)",
      "linea": 5,
      "columna": 1
    },
    {
      "mensaje": "[CODIGO_ACTIVIDAD_VALIDO] CUIL 20123456789: CODACTIVIDAD = 99 no es válido (valores permitidos: 19, 32, 45, 52, 65, 76, 77, 85, 913)",
      "linea": 25,
      "columna": 8
    },
    {
      "mensaje": "[TIPO_EMPRESA_VALIDO] CUIL 20987654321: TIPOEMPRESA = 'X' no es válido (valores permitidos: '3', 'G')",
      "linea": 30,
      "columna": 38
    }
  ],
  "advertencias": [
    {
      "mensaje": "[RANGOS_CAMPOS] CUIL 20555666777: CANTHIJOS = 25 parece inusual (rango esperado: 0-20)",
      "linea": 100,
      "columna": 5
    },
    {
      "mensaje": "[CONSISTENCIA_CAMPOS] CUIL 20111222333: Tiene asignaciones familiares pagadas pero 0 hijos declarados",
      "linea": 150,
    "columna": 16
    }
  ]
}
```

---

## ?? Agregar Nueva Regla

### Paso 1: Crear la clase

```csharp
// Validaciones/Rules/MiNuevaReglaRule.cs
public class MiNuevaReglaRule : ValidacionRuleBase
{
    public override string NombreRegla => "MI_REGLA";
    public override string Descripcion => "Descripción clara";
    public override TipoValidacion Tipo => TipoValidacion.Error;
    public override int Orden => 99;

    protected override async Task<List<DetalleValidacionDto>> 
    EjecutarValidacionAsync(IDbConnection connection, long idArchivo)
    {
    var sql = "SELECT ...";
      var resultados = await connection.QueryAsync<MiDto>(sql);
        return resultados.Select(r => CrearDetalle(...)).ToList();
    }
}
```

### Paso 2: Registrar en DI

```csharp
// Startup.cs
services.AddScoped<IValidacionRule, MiNuevaReglaRule>();
```

### Paso 3: ¡Listo! ?

---

## ?? Estadísticas

| Tipo | Cantidad | Porcentaje |
|------|----------|------------|
| **Errores** | 8 | 80% |
| **Advertencias** | 2 | 20% |
| **Total** | 10 | 100% |

---

## ? Campos Validados

### Identificación
- ? CUIL (formato, duplicados, obligatorio)
- ? APENOM (obligatorio, no vacío)

### Códigos
- ? CODACTIVIDAD (valores permitidos: 19, 32, 45, 52, 65, 76, 77, 85, 913)
- ? CODCONDICION (valores permitidos: 1, 2, 5)
- ? CODSITUACION (obligatorio)
- ? TIPOEMPRESA (valores permitidos: '3', 'G')

### Remuneraciones
- ? REMUNIMPONIBLE1-11 (>= 0, al menos una > 0)
- ? REMUNIMPONIBLE4 (> 0 para actividades 46 y 77 - Obra Social Nacional)

### Cantidades
- ?? CANTHIJOS (rango: 0-20)
- ?? CANTADHERENTES (rango: 0-10)
- ?? CANT_DIAS_TRABA (rango: 0-31)
- ?? CANTHORASEXTRA (rango: 0-200)
- ?? HORASTRAB (rango: 0-400)

### Consistencia
- ?? Cónyuge vs Hijos
- ?? Asignaciones familiares vs Hijos
- ?? Días trabajados vs Remuneraciones
- ?? Horas extra (monto vs cantidad)

---

**?? Documentación completa disponible en:**
- [README.md](README.md) - Índice general
- [GUIA_RAPIDA.md](GUIA_RAPIDA.md) - Ejemplos prácticos
- [CORRECCION_GTT.md](CORRECCION_GTT.md) - Corrección basada en estructura real
- [EJEMPLOS_API.md](EJEMPLOS_API.md) - Ejemplos de respuestas API
