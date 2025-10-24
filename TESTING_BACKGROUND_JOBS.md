# Testing Background Jobs - Guía de Pruebas

## Setup Inicial

### 1. Ejecutar Script de Oracle

Ejecutar el script para crear los stored procedures de prueba:

```bash
# Conectarse a Oracle y ejecutar
sqlplus SEGSOCIAL/password@HTEST01 @Scripts/Oracle_TestJobs_Setup.sql
```

O manualmente en SQL Developer/Toad ejecutar el contenido de `Scripts/Oracle_TestJobs_Setup.sql`.

**Nota**: El script es compatible con **Oracle 11g** y usa `CONNECT BY LEVEL` en lugar de `DBMS_LOCK.SLEEP` para la simulación de pausas.

### 2. Verificar Configuración

Asegurarse que `appsettings.json` tenga configuración de timeout:

```json
{
  "BackgroundJobs": {
    "TimeoutMinutes": 60,
    "ResultExpirationHours": 1
  }
}
```

## Endpoints de Prueba

### 1. Job Rápido (30 segundos)

**Iniciar:**
```http
POST /api/jobs/test/quick
```

**Respuesta:**
```json
{
  "job_id": "abc123...",
  "message": "Job de prueba iniciado",
  "duration": "30 segundos"
}
```

### 2. Job Lento (2 minutos)

**Iniciar:**
```http
POST /api/jobs/test/slow
```

**Respuesta:**
```json
{
  "job_id": "def456...",
  "message": "Job de prueba iniciado", 
  "duration": "2 minutos"
}
```

## Monitoreo del Progreso

### 1. Consultar Estado

```http
GET /api/jobs/{jobId}
```

**Respuesta durante ejecución:**
```json
{
  "job_id": "abc123...",
  "status": 1,
  "status_message": "Procesando paso 5 de 15 (33%)",
  "progress_percentage": 33,
  "created_at": "2025-10-24T10:00:00Z",
  "started_at": "2025-10-24T10:00:02Z",
  "completed_at": null,
  "result": null,
  "error_message": null
}
```

### 2. Obtener Resultado

Cuando `status === 2` (Completed):

```http
GET /api/jobs/{jobId}/result
```

**Respuesta:**
```json
"TEST_COMPLETED_20251024103045_STEPS_15"
```

## Comportamiento Esperado

### Job Rápido (30 segundos)
- **Duración:** 30 segundos
- **Pasos:** 15 (cada 2 segundos)
- **Progreso:** 0% → 6% → 13% → ... → 100%
- **Mensajes:** "Procesando paso X de 15 (Y%)"

### Job Lento (2 minutos) 
- **Duración:** 120 segundos
- **Pasos:** 20 (cada 6 segundos)
- **Progreso:** 0% → 5% → 10% → ... → 100%
- **Mensajes:** "Procesando paso X de 20 (Y%)"

## Testing Frontend

### Ejemplo JavaScript

```javascript
// 1. Iniciar job de prueba
const response = await fetch('/api/jobs/test/quick', { method: 'POST' });
const { job_id } = await response.json();

console.log('Job iniciado:', job_id);

// 2. Polling cada 2 segundos
const pollProgress = async () => {
  const statusResponse = await fetch(`/api/jobs/${job_id}`);
  const jobInfo = await statusResponse.json();
  
  console.log(`Progreso: ${jobInfo.progress_percentage}% - ${jobInfo.status_message}`);
  
  if (jobInfo.status === 2) {
    // Completado
    const resultResponse = await fetch(`/api/jobs/${job_id}/result`);
    const result = await resultResponse.json();
    console.log('Resultado:', result);
    return result;
  }
  
  if (jobInfo.status === 3) {
    // Error
    console.error('Job falló:', jobInfo.error_message);
    return;
  }
  
  // Continuar polling
  setTimeout(pollProgress, 2000);
};

pollProgress();
```

### Ejemplo cURL

```bash
# 1. Iniciar job
curl -X POST http://localhost:5000/api/jobs/test/quick

# 2. Consultar progreso (repetir cada pocos segundos)
curl http://localhost:5000/api/jobs/{jobId}

# 3. Obtener resultado cuando complete
curl http://localhost:5000/api/jobs/{jobId}/result

# 4. Cancelar si es necesario
curl -X POST http://localhost:5000/api/jobs/{jobId}/cancel
```

## Casos de Prueba

### ✅ Caso 1: Job Exitoso
1. POST `/api/jobs/test/quick`
2. Polling cada 2s en `/api/jobs/{id}`
3. Ver progreso incrementar: 0% → 100%
4. Estado cambia a `Completed` (2)
5. GET `/api/jobs/{id}/result` retorna resultado

### ✅ Caso 2: Cancelación Manual
1. POST `/api/jobs/test/slow`  
2. Polling hasta ~50% progreso
3. POST `/api/jobs/{id}/cancel`
4. Estado cambia a `Cancelled` (4)

### ✅ Caso 3: Timeout (si configurado < 2 min)
1. Configurar `TimeoutMinutes: 1` en appsettings
2. POST `/api/jobs/test/slow`
3. Job se cancela automáticamente después de 1 minuto
4. Estado cambia a `Failed` (3) con mensaje de timeout

### ✅ Caso 4: Sin Timeout
1. Configurar `TimeoutMinutes: 0`
2. POST `/api/jobs/test/slow`
3. Job completa normalmente sin cancelación automática

## Limpieza

### Limpiar jobs de prueba manualmente:

```sql
-- En Oracle
EXEC SEGSOCIAL.SP_CLEANUP_TEST_JOBS;
```

### O mediante endpoint (si se implementa):

```http
POST /api/jobs/test/cleanup
```

## Troubleshooting

### Error "Table or view does not exist"
- Verificar que se ejecutó el script SQL correctamente
- Verificar que el usuario REPORTES tiene permisos

### Jobs se quedan en "Pending"
- Verificar que el BackgroundJobExecutor está funcionando
- Revisar logs de la aplicación

### Progreso no actualiza
- Verificar conectividad con Oracle
- Verificar que el polling esté funcionando
- Revisar tabla SEGSOCIAL.JOB_PROGRESS directamente

### Timeout inesperado
- Revisar configuración `BackgroundJobs:TimeoutMinutes`
- Para pruebas, configurar timeout alto o 0 (sin timeout)