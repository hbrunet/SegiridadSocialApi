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

## Manejo de Errores

### Job no encontrado o expirado
```json
{
  "error": "Job no encontrado o expirado"
}
```
**Causa**: El jobId no existe o pasó el tiempo de expiración (1 hora).  
**Solución**: Reiniciar el proceso.

### Job con error
```json
{
  "job_id": "...",
  "status": 3,
  "error_message": "ORA-12345: descripción del error"
}
```
**Causa**: El SP falló durante la ejecución.  
**Solución**: Revisar logs, validar datos, reintentar.

### Timeout excedido
```json
{
  "error_message": "Job cancelado o timeout excedido"
}
```
**Causa**: El job tardó más del tiempo configurado en `BackgroundJobs:TimeoutMinutes`.  
**Solución**: 
- Revisar performance del SP en Oracle
- Aumentar el timeout en `appsettings.json` si el SP legítimamente necesita más tiempo
- Optimizar el SP para que ejecute más rápido

## Comparación: Sync vs Async

### Sync (endpoint original `/crear-hoja`)
✅ Respuesta inmediata  
✅ Más simple para SPs rápidos (<30s)  
❌ No apto para SPs largos (>1 minuto)  
❌ Timeout de HTTP/proxy

### Async (endpoint nuevo `/crear-hoja-async`)
✅ No bloquea la conexión HTTP  
✅ Progreso en tiempo real  
✅ Soporte para SPs largos (hasta el timeout configurado)  
✅ Mejor experiencia de usuario  
❌ Requiere polling del cliente  
❌ Un poco más complejo

## Recomendación

- **SPs rápidos (<30s)**: Usar `/crear-hoja` (sync)
- **SPs lentos (>1 min)**: Usar `/crear-hoja-async` (async)
- **SPs muy lentos (>10 min)**: Usar `/crear-hoja-async` obligatorio

## Reporte de Progreso desde Oracle

### ¿Cómo funciona?

Los stored procedures de Oracle **no pueden hacer callbacks directos** a .NET. En su lugar:

1. La API crea un registro en la tabla `USUARIO.JOB_PROGRESS` al iniciar el job
2. El SP actualiza periódicamente esta tabla con su progreso:
   ```sql
   UPDATE USUARIO.JOB_PROGRESS 
   SET PROGRESS_PCT = 50, STATUS_MESSAGE = 'Procesando registros...'
   WHERE JOB_ID = :JobId;
   COMMIT; -- Importante para que sea visible
   ```
3. La API hace **polling cada 2 segundos** a la tabla para leer el progreso
4. El progreso se refleja en el `JobInfoDto` que consulta el cliente

### Setup en Oracle

**1. Crear la tabla de progreso** (ejecutar una sola vez):

```bash
# Aplicar script SQL
sqlplus USUARIO/password@HTEST01 @Scripts/Oracle_JobProgress_Setup.sql
```

O manualmente:
```sql
CREATE TABLE USUARIO.JOB_PROGRESS (
  JOB_ID VARCHAR2(50) PRIMARY KEY,
  PROGRESS_PCT NUMBER(3) DEFAULT 0 CHECK (PROGRESS_PCT BETWEEN 0 AND 100),
  STATUS_MESSAGE VARCHAR2(500),
  LAST_UPDATE TIMESTAMP DEFAULT SYSTIMESTAMP
);
```

**2. Modificar el SP para reportar progreso**:

Ver ejemplo completo en `Scripts/Oracle_JobProgress_Setup.sql`. Patrón básico:

```sql
PROCEDURE MI_SP_LARGO(
  vJOB_ID IN VARCHAR2,  -- ← Agregar este parámetro
  -- otros parámetros...
) AS
BEGIN
  -- Paso 1 (10%)
  UPDATE USUARIO.JOB_PROGRESS 
  SET PROGRESS_PCT = 10, STATUS_MESSAGE = 'Validando...'
  WHERE JOB_ID = vJOB_ID;
  COMMIT;
  
  -- ... lógica paso 1 ...
  
  -- Paso 2 (50%)
  UPDATE USUARIO.JOB_PROGRESS 
  SET PROGRESS_PCT = 50, STATUS_MESSAGE = 'Procesando...'
  WHERE JOB_ID = vJOB_ID;
  COMMIT;
  
  -- ... etc ...
END;
```

**Importante**: Hacer `COMMIT` después de cada UPDATE para que la API vea el cambio inmediatamente.

### Si no se puede modificar el SP

Si el SP no puede ser modificado para reportar progreso:
- El job seguirá funcionando, pero el progreso se quedará en 0%
- La API mostrará "Procesando..." hasta que el SP complete
- El cliente debe seguir haciendo polling hasta que `status === 2` (Completed)

## Escalabilidad

El sistema actual usa **in-memory storage** (IMemoryCache) que funciona para:
- Deployment single-instance
- Volúmenes bajos-medios de jobs concurrentes

Para **alta disponibilidad** o **múltiples instancias**, migrar a:
- Redis para storage distribuido
- Hangfire/Quartz para job scheduling robusto
- Azure Service Bus / RabbitMQ para cola persistente
