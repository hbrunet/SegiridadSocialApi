# Background Jobs para Stored Procedures de Larga Duración

## Problema Resuelto

Algunos stored procedures de Oracle pueden tardar **más de 10 minutos** en ejecutarse. Mantener una conexión HTTP abierta tanto tiempo causa:
- Timeouts en el cliente
- Timeouts en proxies/load balancers
- Recursos del servidor bloqueados
- Mala experiencia de usuario

## Solución Implementada

Sistema de **Background Jobs** que permite:
- Iniciar el SP de forma asíncrona
- Consultar el progreso en tiempo real
- Obtener el resultado cuando complete
- Cancelar jobs en ejecución
- Timeout automático configurable (default: 60 minutos)

## Arquitectura

```
Cliente                    API                         Background Worker
  |                         |                                  |
  |--POST /crear-hoja-async->|                                  |
  |<-----202 + jobId---------|                                  |
  |                         |--Encolar Job------------------>|
  |                         |                                  |
  |                         |                              [Ejecuta SP]
  |                         |                                  |
  |--GET /jobs/{jobId}----->|                                  |
  |<-----Estado + %----------|                                  |
  |                         |                                  |
  |   (polling cada 5s)     |                              [Completa]
  |                         |                                  |
  |--GET /jobs/{jobId}----->|                                  |
  |<-----Completed-----------|                                  |
  |                         |                                  |
  |--GET /jobs/{jobId}/result->|                               |
  |<-----Resultado-----------|                                  |
```

## Uso desde el Frontend

### 1. Iniciar Job

**Endpoint alternativo para SPs largos**:

```http
POST /api/novedades/crear-hoja-async
Content-Type: application/json

{
  "id_archivo": 12345,
  "tipo_novedad": 156,
  "grupo_adicional": 1,
  "tipo_liquidacion": 2,
  "cantidad_registros": 100,
  "periodo": "2025-07-01T00:00:00",
  "id_rep": 999,
  "flow_id": "opcional-si-viene-de-upload"
}
```

**Respuesta** (202 Accepted):
```json
{
  "job_id": "f3a2c1b5d4e3f2a1b5c4d3e2f1a0b9c8",
  "message": "Creación de hoja iniciada en background. Use el jobId para consultar el estado."
}
```

### 2. Consultar Estado (Polling)

**Consultar cada 5 segundos** hasta que el estado sea `Completed` o `Failed`:

```http
GET /api/jobs/f3a2c1b5d4e3f2a1b5c4d3e2f1a0b9c8
```

**Respuesta**:
```json
{
  "job_id": "f3a2c1b5d4e3f2a1b5c4d3e2f1a0b9c8",
  "status": 1,  // 0=Pending, 1=Running, 2=Completed, 3=Failed, 4=Cancelled
  "status_message": "Crear hoja para archivo 12345",
  "progress_percentage": 65,
  "created_at": "2025-10-23T10:00:00Z",
  "started_at": "2025-10-23T10:00:02Z",
  "completed_at": null,
  "result": null,
  "error_message": null,
  "elapsed_time": null
}
```

### 3. Obtener Resultado

Cuando `status === 2` (Completed):

```http
GET /api/jobs/f3a2c1b5d4e3f2a1b5c4d3e2f1a0b9c8/result
```

**Respuesta**:
```json
{
  "id_archivo": 12345,
  "tipo_novedad": 156,
  "grupo_adicional": 1,
  "tipo_liquidacion": 2,
  "nro_hoja": 5001
}
```

### 4. Cancelar Job (Opcional)

```http
POST /api/jobs/f3a2c1b5d4e3f2a1b5c4d3e2f1a0b9c8/cancel
```

## Estados del Job

| Código | Estado | Descripción |
|--------|--------|-------------|
| 0 | Pending | En cola esperando ejecución |
| 1 | Running | Ejecutándose actualmente |
| 2 | Completed | Completado exitosamente |
| 3 | Failed | Falló con error |
| 4 | Cancelled | Cancelado manualmente |

## Ejemplo Frontend (JavaScript)

```javascript
// 1. Iniciar job
const response = await fetch('/api/novedades/crear-hoja-async', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(request)
});

const { job_id } = await response.json();

// 2. Polling hasta completar
const pollJob = async () => {
  const statusResponse = await fetch(`/api/jobs/${job_id}`);
  const jobInfo = await statusResponse.json();
  
  // Actualizar UI con progreso
  updateProgress(jobInfo.progress_percentage);
  
  if (jobInfo.status === 2) { // Completed
    const resultResponse = await fetch(`/api/jobs/${job_id}/result`);
    const result = await resultResponse.json();
    console.log('Hoja creada:', result.nro_hoja);
    return result;
  }
  
  if (jobInfo.status === 3) { // Failed
    throw new Error(jobInfo.error_message);
  }
  
  if (jobInfo.status === 1 || jobInfo.status === 0) { // Running o Pending
    // Reintentar en 5 segundos
    await new Promise(resolve => setTimeout(resolve, 5000));
    return pollJob();
  }
};

const result = await pollJob();
```

## Timeouts y Limpieza

- **Timeout de ejecución**: Configurable en `appsettings.json` (default: 60 minutos)
- **Expiración de resultados**: 1 hora después de completar
- **Expiración de jobs fallidos**: 1 hora
- **Limpieza automática**: Memory cache limpia entries expiradas
- **Auditoría permanente**: Todos los jobs se registran en `SEGSOCIAL.JOB_AUDIT` para trazabilidad

### Configuración del Timeout

En `appsettings.json`:

```json
{
  "BackgroundJobs": {
    "TimeoutMinutes": 60,
    "ResultExpirationHours": 1
  }
}
```

**Valores recomendados**:
- **SPs rápidos (<5 min)**: `TimeoutMinutes: 10`
- **SPs normales (5-30 min)**: `TimeoutMinutes: 60` (default)
- **SPs muy largos (>30 min)**: `TimeoutMinutes: 120` o más
- **Sin timeout automático**: `TimeoutMinutes: 0` (solo cancelación manual)

**Importante**: 
- Si un SP excede el timeout configurado, se cancelará automáticamente y el job se marcará como `Failed` con el mensaje "Job cancelado o timeout excedido".
- Si configuras `TimeoutMinutes: 0`, el job **NO tendrá timeout automático** y solo podrá cancelarse manualmente mediante `POST /api/jobs/{id}/cancel`.
- **Todos los jobs se auditan en Oracle**: Los resultados in-memory expiran, pero el registro permanente queda en `SEGSOCIAL.JOB_AUDIT`.

## Sistema de Auditoría

### ¿Por qué Auditar?

Los resultados en **memory cache expiran** después de 1 hora, pero necesitas:
- ✅ **Trazabilidad**: Registro permanente de todas las ejecuciones
- ✅ **Compliance**: Auditoría de quién ejecutó qué y cuándo
- ✅ **Análisis**: Métricas de performance, tendencias de errores
- ✅ **Debugging**: Logs detallados para troubleshooting
- ✅ **Reportes**: Dashboards de gestión

### Registro Automático

**Todos los jobs se auditan automáticamente** en Oracle:

```sql
SEGSOCIAL.JOB_AUDIT
├── JOB_ID (ID del JobManager .NET)
├── INTERNAL_JOB_ID (ID de JOB_PROGRESS Oracle)
├── JOB_NAME (Descripción)
├── JOB_TYPE (FUSION, VALIDACION, etc.)
├── INPUT_PARAMS (JSON con parámetros)
├── CREATED_AT / STARTED_AT / COMPLETED_AT
├── STATUS (COMPLETED, FAILED, TIMEOUT, CANCELLED)
├── RESULT_DATA (JSON con resultado completo)
├── ERROR_MESSAGE (si falló)
├── DURATION_SECONDS
├── REGISTROS_PROCESADOS / REGISTROS_ERRORES
└── USUARIO / IP_ADDRESS / USER_AGENT
```

### Consultar Auditoría

**Desde la API**:
```http
GET /api/jobs/{jobId}/audit      # Registro completo
GET /api/jobs/{jobId}/logs       # Logs detallados
```

**Desde Oracle**:
```sql
-- Últimos 20 jobs
SELECT * FROM SEGSOCIAL.VW_JOB_AUDIT_SUMMARY
ORDER BY CREATED_AT DESC FETCH FIRST 20 ROWS ONLY;

-- Jobs fallidos hoy
SELECT * FROM SEGSOCIAL.VW_JOB_FAILURES;

-- Estadísticas por tipo
SELECT * FROM SEGSOCIAL.VW_JOB_STATS;
```

### Setup de Auditoría

```bash
sqlplus SEGSOCIAL/password@HTEST01 @Scripts/Oracle_Job_Audit_Setup.sql
```

Ver documentación completa en: **`Docs/JOB_AUDIT_SYSTEM.md`**
