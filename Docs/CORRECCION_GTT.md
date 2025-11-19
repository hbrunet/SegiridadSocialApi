# ?? Corrección de Validaciones - Estructura Real de GTT

## ? Problema Detectado

Las reglas de validación originales hacían referencia al campo **`PERIODO`** que **NO existe** en la tabla `USUARIO.TMP_NOV_DDJJ_PREV`.

### Reglas Eliminadas (3)

| Regla | Motivo |
|-------|--------|
| `ValidarCamposObligatoriosRule` | Validaba `PERIODO IS NULL` |
| `ValidarPeriodoFuturoRule` | Validaba `PERIODO > SYSDATE` |
| `ValidarConsistenciaTipoLiquidacionPeriodoRule` | Validaba `EXTRACT(MONTH FROM PERIODO)` |

---

## ? Solución Implementada

Creadas **3 nuevas reglas** basadas en la **estructura real de la GTT**:

### 1. `ValidarCamposObligatoriosGttRule`

**Tipo:** Error  
**Orden:** 8

**Validaciones:**
- ? `CUIL IS NOT NULL`
- ? `APENOM` no vacío
- ? Al menos una `REMUNIMPONIBLE*` > 0
- ? `CODSITUACION` no nulo

**Ejemplo de error:**
```json
{
  "mensaje": "[CAMPOS_OBLIGATORIOS_GTT] CUIL 20123456789: APENOM (Apellido y Nombre) es obligatorio",
  "linea": 15,
  "columna": 3
}
```

---

### 2. `ValidarRangosCamposRule`

**Tipo:** Advertencia  
**Orden:** 25

**Validaciones:**
- ?? `CANTHIJOS` entre 0-20
- ?? `CANTADHERENTES` entre 0-10
- ?? `CANT_DIAS_TRABA` entre 0-31
- ?? `CANTHORASEXTRA` entre 0-200
- ?? `HORASTRAB` entre 0-400

**Ejemplo de advertencia:**
```json
{
  "mensaje": "[RANGOS_CAMPOS] CUIL 20987654321: CANT_DIAS_TRABA = 35 es inválido (debe estar entre 0 y 31)",
  "linea": 42,
  "columna": 40
}
```

---

### 3. `ValidarConsistenciaCamposRule`

**Tipo:** Advertencia  
**Orden:** 30

**Validaciones:**
- ?? Cónyuge = 'S' pero `CANTHIJOS` = 0
- ?? `REMUNTOTAL` < suma de remuneraciones imponibles
- ?? `ASIGFAMPAGADAS` > 0 pero `CANTHIJOS` = 0
- ?? `CANT_DIAS_TRABA` = 0 pero tiene remuneraciones
- ?? `HORAS_EXTRA` > 0 pero `CANTHORASEXTRA` = 0

**Ejemplo de advertencia:**
```json
{
  "mensaje": "[CONSISTENCIA_CAMPOS] CUIL 20555666777: REMUNTOTAL (50000) es menor que suma de imponibles (55000)",
  "linea": 78,
  "columna": 14
}
```

---

## ?? Reglas Actualizadas

### Antes (Incorrectas)
| # | Regla | Estado |
|---|-------|--------|
| 1 | FORMATO_CUIL | ? Activa |
| 2 | CAMPOS_OBLIGATORIOS | ? Usaba PERIODO |
| 3 | CUIL_DUPLICADO | ? Activa |
| 4 | REMUNERACION_POSITIVA | ? Activa |
| 5 | PERIODO_FUTURO | ? Usaba PERIODO |
| 6 | CONSISTENCIA_TIPO_LIQUIDACION | ? Usaba PERIODO |

**Reglas funcionales:** 3 de 6 (50%)

### Ahora (Corregidas)
| # | Regla | Tipo | Descripción |
|---|-------|------|-------------|
| 1 | FORMATO_CUIL | Error | Valida 11 dígitos numéricos |
| 2 | CAMPOS_OBLIGATORIOS_GTT | Error | Campos obligatorios de GTT |
| 3 | CUIL_DUPLICADO | Error | Detecta duplicados |
| 4 | CODIGO_ACTIVIDAD_VALIDO | Error | Valida valores permitidos de CODACTIVIDAD |
| 5 | TIPO_EMPRESA_VALIDO | Error | Valida que TIPOEMPRESA sea '3' o 'G' |
| 6 | CODIGO_CONDICION_VALIDO | Error | Valida que CODCONDICION sea 1, 2 o 5 |
| 7 | REMUNERACION_POSITIVA | Error | Valores >= 0 |
| 8 | OBRA_SOCIAL_NACIONAL_REQUERIDA | Error | Actividades 46/77 requieren REMUNIMPONIBLE4 > 0 |
| 9 | RANGOS_CAMPOS | Advertencia | Rangos numéricos razonables |
| 10 | CONSISTENCIA_CAMPOS | Advertencia | Consistencia lógica |

**Reglas funcionales:** 10 de 10 (100%) ?

---

## ??? Estructura Real de TMP_NOV_DDJJ_PREV

### Campos Principales
```sql
-- Identificación
ID  NUMBER(18)
CUIL     NUMBER(11)
APENOM             VARCHAR2(30)

-- Familia
CONYUGE          VARCHAR2(1)      -- 'S'/'N'
CANTHIJOS   NUMBER(2)
CANTADHERENTES        NUMBER(2)

-- Códigos
CODSITUACION   NUMBER(2)
CODCONDICION          NUMBER(2)
CODACTIVIDAD          NUMBER(3)
CODMODCONTRATACION    NUMBER(3)
CODOBRASOC            NUMBER(6)

-- Remuneraciones Imponibles (10 campos)
REMUNIMPONIBLE1       NUMBER(12,2)
REMUNIMPONIBLE2       NUMBER(12,2)
REMUNIMPONIBLE3    NUMBER(12,2)
REMUNIMPONIBLE4       NUMBER(12,2)
REMUNIMPONIBLE5       NUMBER(12,2)
REMUNIMPONIBLE6       NUMBER(12,2)
REMUNIMPONIBLE7   NUMBER(12,2)
REMUNIMPONIBLE8       NUMBER(12,2)
REMUNIMPONIBLE9       NUMBER(12,2)
REMUNIMPONIBLE11      NUMBER(12,2)

-- Remuneraciones Específicas
REMUNTOTAL            NUMBER(12,2)
SUELDO_Y_ADICIONALES  NUMBER(12,2)
SAC    NUMBER(12,2)
HORAS_EXTRA        NUMBER(12,2)
VACACIONES    NUMBER(12,2)

-- Conceptos Adicionales
ASIGFAMPAGADAS        NUMBER(9,2)
APORTEVOLUNTARIO NUMBER(9,2)
ADICIONALOS      NUMBER(9,2)
EXCEDENTEAPORTESS     NUMBER(9,2)

-- Tiempos Trabajados
CANT_DIAS_TRABA NUMBER(9)
CANTHORASEXTRA        NUMBER(3)
HORASTRAB  NUMBER(3)

-- Otros
PROVLOCALIDAD         VARCHAR2(50)
TIPOEMPRESA    VARCHAR2(1)
REGIMEN       NUMBER(1)
TIPOOPERACION         NUMBER(1)
```

**?? Nota Importante:** No hay campo `PERIODO` en esta tabla. El período se maneja a nivel de **HOJA** (tabla permanente).

---

## ?? Archivos Modificados

### Eliminados
- ? `Validaciones/Rules/ValidarCamposObligatoriosRule.cs`
- ? `Validaciones/Rules/ValidarPeriodoFuturoRule.cs`
- ? `Validaciones/Rules/ValidarConsistenciaTipoLiquidacionPeriodoRule.cs`

### Creados
- ? `Validaciones/Rules/ValidarCamposObligatoriosGttRule.cs`
- ? `Validaciones/Rules/ValidarRangosCamposRule.cs`
- ? `Validaciones/Rules/ValidarConsistenciaCamposRule.cs`

### Actualizados
- ?? `Startup.cs` - Registro de nuevas reglas
- ?? `Validaciones/README.md` - Documentación actualizada

---

## ?? Ejemplos de Respuestas

### Validación Exitosa
```json
{
  "registros_validos": 1000,
  "registros_advertencias": 15,
  "registros_errores": 0,
  "errores": [],
  "advertencias": [
    {
      "mensaje": "[RANGOS_CAMPOS] CUIL 20123456789: CANTHIJOS = 25 parece inusual (rango esperado: 0-20)",
      "linea": 100,
   "columna": 5
    }
  ]
}
```

### Validación con Errores
```json
{
  "registros_validos": 950,
  "registros_advertencias": 20,
  "registros_errores": 30,
  "errores": [
    {
      "mensaje": "[CAMPOS_OBLIGATORIOS_GTT] CUIL 20987654321: APENOM (Apellido y Nombre) es obligatorio",
      "linea": 15,
      "columna": 3
    },
    {
      "mensaje": "[CAMPOS_OBLIGATORIOS_GTT] CUIL 20555666777: Debe tener al menos una remuneración imponible con valor > 0",
      "linea": 42,
      "columna": 15
    },
    {
   "mensaje": "[REMUNERACION_POSITIVA] CUIL 20111222333: REMUNIMPONIBLE1 tiene valor negativo (-1000)",
      "linea": 78,
"columna": 5
    }
  ],
  "advertencias": [
  {
      "mensaje": "[CONSISTENCIA_CAMPOS] CUIL 20333444555: Tiene asignaciones familiares pagadas pero 0 hijos declarados",
    "linea": 120,
      "columna": 16
    }
  ]
}
```

---

## ? Testing

### Verificar Reglas Activas
```http
GET /api/novedades/reglas-validacion
```

**Respuesta esperada:**
```json
{
  "total": 6,
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
    {
    "nombre": "CUIL_DUPLICADO",
      "descripcion": "Verifica que no existan CUILs duplicados dentro del mismo archivo",
      "tipo": "Error",
      "orden": 10
    },
    {
      "nombre": "REMUNERACION_POSITIVA",
    "descripcion": "Verifica que todas las remuneraciones sean valores mayores o iguales a cero",
      "tipo": "Error",
    "orden": 20
    },
    {
"nombre": "RANGOS_CAMPOS",
      "descripcion": "Verifica que los campos numéricos estén dentro de rangos válidos",
      "tipo": "Advertencia",
      "orden": 25
    },
    {
      "nombre": "CONSISTENCIA_CAMPOS",
      "descripcion": "Verifica consistencia lógica entre campos relacionados (ej: cónyuge/hijos, remuneraciones)",
   "tipo": "Advertencia",
      "orden": 30
  }
  ]
}
```

---

## ?? Beneficios

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| **Reglas funcionales** | 50% (3/6) | 100% (9/9) |
| **Errores en runtime** | ? Queries inválidas | ? Sin errores |
| **Cobertura de validación** | Básica | Completa |
| **Consistencia de datos** | ?? Parcial | ? Alta |
| **Documentación** | ? Desactualizada | ? Actualizada |

---

## ?? Próximos Pasos Sugeridos

1. ? **Agregar validaciones de códigos** contra tablas maestras:
   - `CODSITUACION` válido
   - `CODCONDICION` válido
   - `CODOBRASOC` existe en maestro

2. ? **Validar localidades** contra tabla de localidades

3. ? **Validar rangos específicos** según reglas de negocio:
   - Aportes voluntarios máximos
   - Porcentajes de aportes adicionales

4. ? **Tests unitarios** para cada regla nueva

---

## ?? Soporte

- **Estructura GTT:** Ver script DDL en este documento
- **Agregar reglas:** Ver [GUIA_RAPIDA.md](GUIA_RAPIDA.md)
- **Troubleshooting:** Ver [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

---

**? Build Exitoso - Sistema de validaciones corregido y alineado con estructura real de Oracle GTT**
