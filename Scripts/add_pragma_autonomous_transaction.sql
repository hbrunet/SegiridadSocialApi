-- ============================================================================
-- Scripts para Agregar PRAGMA AUTONOMOUS_TRANSACTION a Procedimientos de Auditoría
-- Schema: SEGSOCIAL
-- Package: JOBS_MONITOR
-- ============================================================================

-- IMPORTANTE: Ejecutar estos scripts en orden
-- Hacer backup antes de ejecutar en producción

-- ============================================================================
-- 1. BACKUP - Guardar versiones actuales
-- ============================================================================

-- Ejecutar estos SELECTs y guardar el output antes de modificar
SELECT TEXT 
FROM ALL_SOURCE 
WHERE OWNER = 'SEGSOCIAL' 
  AND NAME = 'JOB_AUDIT_INSERT'
  AND TYPE = 'PROCEDURE'
ORDER BY LINE;

SELECT TEXT 
FROM ALL_SOURCE 
WHERE OWNER = 'SEGSOCIAL' 
  AND NAME = 'JOB_AUDIT_START'
  AND TYPE = 'PROCEDURE'
ORDER BY LINE;

SELECT TEXT 
FROM ALL_SOURCE 
WHERE OWNER = 'SEGSOCIAL' 
  AND NAME = 'JOB_AUDIT_COMPLETE'
  AND TYPE = 'PROCEDURE'
ORDER BY LINE;

SELECT TEXT 
FROM ALL_SOURCE 
WHERE OWNER = 'SEGSOCIAL' 
  AND NAME = 'JOB_AUDIT_LOG'
  AND TYPE = 'PROCEDURE'
ORDER BY LINE;

-- ============================================================================
-- 2. JOB_AUDIT_INSERT - Insertar registro inicial de auditoría
-- ============================================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_INSERT(
    p_job_id IN VARCHAR2,
    p_endpoint IN VARCHAR2,
    p_input_params IN CLOB,
    p_created_by IN VARCHAR2,
    p_audit_id OUT NUMBER
) AS
    -- ✅ AGREGADO: Transacción autónoma para persistir auditoría independientemente
    PRAGMA AUTONOMOUS_TRANSACTION;
BEGIN
INSERT INTO SEGSOCIAL.JOB_AUDIT (
        JOB_ID,
     ENDPOINT,
        INPUT_PARAMS,
        STATUS,
        PROGRESS_PCT,
        CREATED_BY,
        CREATED_AT,
    MODIFIED_AT
  ) VALUES (
        p_job_id,
        p_endpoint,
        p_input_params,
        'PENDIENTE',
        0,
     p_created_by,
        SYSTIMESTAMP,
        SYSTIMESTAMP
    ) RETURNING AUDIT_ID INTO p_audit_id;
    
    -- ✅ AGREGADO: COMMIT requerido por transacción autónoma
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
        -- ✅ MODIFICADO: ROLLBACK solo afecta esta transacción autónoma
      ROLLBACK;
 RAISE;
END JOB_AUDIT_INSERT;
/

-- ============================================================================
-- 3. JOB_AUDIT_START - Marcar job como iniciado
-- ============================================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_START(
    p_job_id IN VARCHAR2
) AS
    -- ✅ AGREGADO: Transacción autónoma para persistir auditoría independientemente
    PRAGMA AUTONOMOUS_TRANSACTION;
BEGIN
    UPDATE SEGSOCIAL.JOB_AUDIT
    SET STARTED_AT = SYSTIMESTAMP,
        STATUS = 'EN EJECUCIÓN',
        MODIFIED_AT = SYSTIMESTAMP
    WHERE JOB_ID = p_job_id;
    
    -- Verificar que el job existe
    IF SQL%ROWCOUNT = 0 THEN
   RAISE_APPLICATION_ERROR(-20001, 'Job ID ' || p_job_id || ' no encontrado en JOB_AUDIT');
 END IF;
    
    -- ✅ AGREGADO: COMMIT requerido por transacción autónoma
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
        -- ✅ MODIFICADO: ROLLBACK solo afecta esta transacción autónoma
        ROLLBACK;
     RAISE;
END JOB_AUDIT_START;
/

-- ============================================================================
-- 4. JOB_AUDIT_COMPLETE - Completar job (éxito, error, timeout, cancelado)
-- ============================================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_COMPLETE(
    p_job_id IN VARCHAR2,
    p_status IN VARCHAR2,
    p_progress_pct IN NUMBER,
  p_result_data IN CLOB DEFAULT NULL,
    p_error_message IN VARCHAR2 DEFAULT NULL
) AS
    -- ✅ AGREGADO: Transacción autónoma para persistir auditoría independientemente
    PRAGMA AUTONOMOUS_TRANSACTION;
    
    v_started_at TIMESTAMP;
    v_duration NUMBER;
BEGIN
    -- Obtener timestamp de inicio para calcular duración
    BEGIN
        SELECT STARTED_AT 
        INTO v_started_at
        FROM SEGSOCIAL.JOB_AUDIT
    WHERE JOB_ID = p_job_id;
    EXCEPTION
   WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20002, 'Job ID ' || p_job_id || ' no encontrado en JOB_AUDIT');
    END;

    -- Calcular duración en segundos (solo si el job fue iniciado)
    IF v_started_at IS NOT NULL THEN
        v_duration := 
          EXTRACT(DAY FROM (SYSTIMESTAMP - v_started_at)) * 86400 +
      EXTRACT(HOUR FROM (SYSTIMESTAMP - v_started_at)) * 3600 +
            EXTRACT(MINUTE FROM (SYSTIMESTAMP - v_started_at)) * 60 +
         EXTRACT(SECOND FROM (SYSTIMESTAMP - v_started_at));
    ELSE
      v_duration := NULL;
    END IF;
    
    -- Actualizar registro de auditoría
    UPDATE SEGSOCIAL.JOB_AUDIT
    SET COMPLETED_AT = SYSTIMESTAMP,
        STATUS = p_status,
        PROGRESS_PCT = p_progress_pct,
        RESULT_DATA = p_result_data,
        ERROR_MESSAGE = p_error_message,
    DURATION_SECONDS = v_duration,
        MODIFIED_AT = SYSTIMESTAMP
    WHERE JOB_ID = p_job_id;
    
    IF SQL%ROWCOUNT = 0 THEN
    RAISE_APPLICATION_ERROR(-20003, 'No se pudo actualizar Job ID ' || p_job_id);
    END IF;
  
    -- ✅ AGREGADO: COMMIT requerido por transacción autónoma
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
        -- ✅ MODIFICADO: ROLLBACK solo afecta esta transacción autónoma
     ROLLBACK;
 RAISE;
END JOB_AUDIT_COMPLETE;
/

-- ============================================================================
-- 5. JOB_AUDIT_LOG - Agregar log detallado durante ejecución
-- ============================================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_LOG(
    p_job_id IN VARCHAR2,
    p_log_level IN VARCHAR2,
    p_log_message IN VARCHAR2,
    p_progress_pct IN NUMBER DEFAULT NULL
) AS
    -- ✅ AGREGADO: Transacción autónoma para persistir logs independientemente
  PRAGMA AUTONOMOUS_TRANSACTION;
BEGIN
    -- Insertar log detallado
    INSERT INTO SEGSOCIAL.JOB_AUDIT_LOGS (
        JOB_ID,
  LOG_TIMESTAMP,
        LOG_LEVEL,
        LOG_MESSAGE,
      PROGRESS_PCT
    ) VALUES (
        p_job_id,
        SYSTIMESTAMP,
        p_log_level,
 p_log_message,
      p_progress_pct
    );
    
    -- Actualizar progreso en la tabla principal si se proporciona
    IF p_progress_pct IS NOT NULL THEN
        UPDATE SEGSOCIAL.JOB_AUDIT
SET PROGRESS_PCT = p_progress_pct,
    MODIFIED_AT = SYSTIMESTAMP
    WHERE JOB_ID = p_job_id;
 END IF;
    
    -- ✅ AGREGADO: COMMIT requerido por transacción autónoma
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
 -- ✅ MODIFICADO: ROLLBACK solo afecta esta transacción autónoma
   -- Si falla el log, no debe afectar el job principal
        ROLLBACK;
        -- No re-lanzamos la excepción para no interrumpir el job
     -- Solo registramos en DBMS_OUTPUT para debugging
        DBMS_OUTPUT.PUT_LINE('Error en JOB_AUDIT_LOG: ' || SQLERRM);
END JOB_AUDIT_LOG;
/

-- ============================================================================
-- 6. VERIFICACIÓN - Comprobar que los procedimientos se compilaron correctamente
-- ============================================================================

-- Verificar que no hay errores de compilación
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM ALL_OBJECTS
WHERE OWNER = 'SEGSOCIAL'
  AND OBJECT_NAME IN ('JOB_AUDIT_INSERT', 'JOB_AUDIT_START', 'JOB_AUDIT_COMPLETE', 'JOB_AUDIT_LOG')
  AND OBJECT_TYPE = 'PROCEDURE';

-- Resultado esperado: STATUS = 'VALID' para todos

-- Ver errores de compilación si los hay
SELECT NAME, TYPE, LINE, POSITION, TEXT
FROM ALL_ERRORS
WHERE OWNER = 'SEGSOCIAL'
  AND NAME IN ('JOB_AUDIT_INSERT', 'JOB_AUDIT_START', 'JOB_AUDIT_COMPLETE', 'JOB_AUDIT_LOG')
ORDER BY NAME, SEQUENCE;

-- ============================================================================
-- 7. PRUEBAS - Scripts de testing
-- ============================================================================

-- Test 1: Job que completa exitosamente
DECLARE
    v_job_id VARCHAR2(50) := 'TEST-JOB-' || TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS');
  v_audit_id NUMBER;
BEGIN
    DBMS_OUTPUT.PUT_LINE('=== Test 1: Job Exitoso ===');
    DBMS_OUTPUT.PUT_LINE('Job ID: ' || v_job_id);
    
    -- Insertar auditoría
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_INSERT(
        v_job_id, '/test/exitoso', '{"test": true}', 'TEST_USER', v_audit_id);
    DBMS_OUTPUT.PUT_LINE('Audit ID: ' || v_audit_id);
    
  -- Iniciar
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_START(v_job_id);
    
 -- Simular progreso
    FOR i IN 1..5 LOOP
        SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_LOG(
         v_job_id, 'INFO', 'Procesando lote ' || i, i * 20);
     DBMS_LOCK.SLEEP(1); -- Esperar 1 segundo
    END LOOP;
 
    -- Completar
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_COMPLETE(
        v_job_id, 'COMPLETADO', 100, '{"resultado": "OK"}', NULL);
    
DBMS_OUTPUT.PUT_LINE('✅ Test 1 PASÓ');
END;
/

-- Verificar resultados
SELECT JOB_ID, STATUS, PROGRESS_PCT, DURATION_SECONDS, CREATED_AT, COMPLETED_AT
FROM SEGSOCIAL.JOB_AUDIT
WHERE JOB_ID LIKE 'TEST-JOB-%'
ORDER BY CREATED_AT DESC
FETCH FIRST 1 ROWS ONLY;

-- Test 2: Job que falla con ROLLBACK (el crítico)
DECLARE
    v_job_id VARCHAR2(50) := 'TEST-FAIL-' || TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS');
    v_audit_id NUMBER;
BEGIN
    DBMS_OUTPUT.PUT_LINE('=== Test 2: Job con ROLLBACK ===');
    DBMS_OUTPUT.PUT_LINE('Job ID: ' || v_job_id);
    
    -- Insertar auditoría
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_INSERT(
        v_job_id, '/test/fallo', '{"test": true}', 'TEST_USER', v_audit_id);
    
-- Iniciar
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_START(v_job_id);
    
    -- Simular trabajo
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_LOG(
        v_job_id, 'INFO', 'Iniciando procesamiento...', 10);
    
    -- Simular error y completar
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_COMPLETE(
        v_job_id, 'ERROR', 10, NULL, 'Error simulado para testing');
    
    -- ❌ ROLLBACK de la transacción principal
    -- Con PRAGMA: La auditoría debe persistir ✅
    -- Sin PRAGMA: La auditoría se perdería ❌
    ROLLBACK;

    DBMS_OUTPUT.PUT_LINE('✅ Test 2 ejecutado - Verificar resultados');
END;
/

-- Verificar que la auditoría se MANTIENE después del ROLLBACK
SELECT JOB_ID, STATUS, PROGRESS_PCT, ERROR_MESSAGE, CREATED_AT, STARTED_AT, COMPLETED_AT
FROM SEGSOCIAL.JOB_AUDIT
WHERE JOB_ID LIKE 'TEST-FAIL-%'
ORDER BY CREATED_AT DESC
FETCH FIRST 1 ROWS ONLY;

-- ✅ Si hay registro: PRAGMA funcionó correctamente
-- ❌ Si no hay registro: PRAGMA no está funcionando

-- Test 3: Job cancelado
DECLARE
    v_job_id VARCHAR2(50) := 'TEST-CANCEL-' || TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS');
    v_audit_id NUMBER;
BEGIN
    DBMS_OUTPUT.PUT_LINE('=== Test 3: Job Cancelado ===');
    
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_INSERT(
        v_job_id, '/test/cancelado', '{"test": true}', 'TEST_USER', v_audit_id);
    
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_START(v_job_id);
    
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_LOG(
     v_job_id, 'INFO', 'Procesando...', 50);
    
    SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_COMPLETE(
        v_job_id, 'CANCELADO', 50, NULL, 'Cancelado por el usuario');
  
    ROLLBACK; -- Simular cancelación
    
    DBMS_OUTPUT.PUT_LINE('✅ Test 3 ejecutado');
END;
/

-- Verificar
SELECT JOB_ID, STATUS, PROGRESS_PCT, ERROR_MESSAGE
FROM SEGSOCIAL.JOB_AUDIT
WHERE JOB_ID LIKE 'TEST-CANCEL-%'
ORDER BY CREATED_AT DESC
FETCH FIRST 1 ROWS ONLY;

-- ============================================================================
-- 8. LIMPIEZA - Eliminar datos de prueba
-- ============================================================================

-- Ejecutar después de verificar que los tests pasaron
DELETE FROM SEGSOCIAL.JOB_AUDIT_LOGS WHERE JOB_ID LIKE 'TEST-%';
DELETE FROM SEGSOCIAL.JOB_AUDIT WHERE JOB_ID LIKE 'TEST-%';
COMMIT;

-- ============================================================================
-- 9. GRANTS - Asegurar permisos correctos
-- ============================================================================

-- Si es necesario, otorgar permisos de ejecución
-- Ajustar según los usuarios/roles de tu sistema
-- GRANT EXECUTE ON SEGSOCIAL.JOBS_MONITOR TO <rol_aplicacion>;

-- ============================================================================
-- FIN DEL SCRIPT
-- ============================================================================

-- Resumen de cambios:
-- 1. ✅ JOB_AUDIT_INSERT  - Agregado PRAGMA AUTONOMOUS_TRANSACTION
-- 2. ✅ JOB_AUDIT_START   - Agregado PRAGMA AUTONOMOUS_TRANSACTION
-- 3. ✅ JOB_AUDIT_COMPLETE - Agregado PRAGMA AUTONOMOUS_TRANSACTION
-- 4. ✅ JOB_AUDIT_LOG     - Agregado PRAGMA AUTONOMOUS_TRANSACTION
--
-- Beneficios:
-- - Auditoría persiste incluso si el job falla
-- - Trazabilidad completa de todos los jobs
-- - Debugging mejorado
-- - Métricas precisas
--
-- Próximos pasos:
-- 1. Revisar los procedimientos actuales
-- 2. Ejecutar este script en DEV
-- 3. Testear exhaustivamente
-- 4. Desplegar a QA/PROD
