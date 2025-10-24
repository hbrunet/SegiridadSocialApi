-- =====================================================
-- SP de Prueba para Testing de Background Jobs
-- Simula un proceso largo con actualizaciones de progreso
-- =====================================================

-- 1. Crear la tabla de progreso si no existe
CREATE TABLE SEGSOCIAL.JOB_PROGRESS (
  JOB_ID VARCHAR2(50) PRIMARY KEY,
  PROGRESS_PCT NUMBER(3) DEFAULT 0 CHECK (PROGRESS_PCT BETWEEN 0 AND 100),
  STATUS_MESSAGE VARCHAR2(500),
  LAST_UPDATE TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- 2. Stored Procedure de prueba principal
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_BACKGROUND_JOB(
  p_job_id IN VARCHAR2,
  p_duration_seconds IN NUMBER DEFAULT 60,
  p_steps IN NUMBER DEFAULT 10,
  p_result OUT VARCHAR2
) AS
  v_step_duration NUMBER;
  v_current_progress NUMBER := 0;
  v_step_increment NUMBER;
BEGIN
  -- Calcular duración por paso y incremento de progreso
  v_step_duration := p_duration_seconds / p_steps;
  v_step_increment := 100 / p_steps;
  
  -- Inicializar progreso
  UPDATE SEGSOCIAL.JOB_PROGRESS 
  SET PROGRESS_PCT = 0, 
      STATUS_MESSAGE = 'Iniciando proceso de prueba...',
      LAST_UPDATE = SYSTIMESTAMP
  WHERE JOB_ID = p_job_id;
  COMMIT;
  
  -- Simular proceso paso a paso
  FOR i IN 1..p_steps LOOP
    -- Simular trabajo (alternativa compatible con Oracle 11g - sin DBMS_LOCK.SLEEP)
    -- Usar CONNECT BY LEVEL para crear una pausa controlada
    DECLARE
      v_iterations NUMBER := GREATEST(1, ROUND(v_step_duration * 100000)); -- Ajustar según duración
      v_dummy NUMBER;
    BEGIN
      SELECT COUNT(*) INTO v_dummy 
      FROM (SELECT LEVEL FROM DUAL CONNECT BY LEVEL <= v_iterations)
      WHERE MOD(LEVEL, 1000) = 0; -- Reducir carga pero mantener tiempo
    END;
    
    -- Actualizar progreso
    v_current_progress := LEAST(i * v_step_increment, 100);
    
    UPDATE SEGSOCIAL.JOB_PROGRESS 
    SET PROGRESS_PCT = v_current_progress,
        STATUS_MESSAGE = 'Procesando paso ' || i || ' de ' || p_steps || ' (' || ROUND(v_current_progress) || '%)',
        LAST_UPDATE = SYSTIMESTAMP
    WHERE JOB_ID = p_job_id;
    COMMIT; -- Importante: hacer commit para que sea visible inmediatamente
    
  END LOOP;
  
  -- Finalizar
  UPDATE SEGSOCIAL.JOB_PROGRESS 
  SET PROGRESS_PCT = 100,
      STATUS_MESSAGE = 'Proceso completado exitosamente',
      LAST_UPDATE = SYSTIMESTAMP
  WHERE JOB_ID = p_job_id;
  COMMIT;
  
  -- Retornar resultado de prueba
  p_result := 'TEST_COMPLETED_' || TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS') || '_STEPS_' || p_steps;
  
EXCEPTION
  WHEN OTHERS THEN
    -- En caso de error, actualizar progreso
    UPDATE SEGSOCIAL.JOB_PROGRESS 
    SET STATUS_MESSAGE = 'ERROR: ' || SQLERRM,
        LAST_UPDATE = SYSTIMESTAMP
    WHERE JOB_ID = p_job_id;
    COMMIT;
    
    RAISE;
END SP_TEST_BACKGROUND_JOB;
/

-- 3. SP rápido para pruebas (30 segundos)
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_QUICK_JOB(
  p_job_id IN VARCHAR2,
  p_result OUT VARCHAR2
) AS
BEGIN
  SP_TEST_BACKGROUND_JOB(
    p_job_id => p_job_id,
    p_duration_seconds => 30,
    p_steps => 15,
    p_result => p_result
  );
END SP_TEST_QUICK_JOB;
/

-- 4. SP lento para pruebas de timeout (2 minutos)
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_SLOW_JOB(
  p_job_id IN VARCHAR2,
  p_result OUT VARCHAR2
) AS
BEGIN
  SP_TEST_BACKGROUND_JOB(
    p_job_id => p_job_id,
    p_duration_seconds => 120,
    p_steps => 20,
    p_result => p_result
  );
END SP_TEST_SLOW_JOB;
/

-- 5. Grants para el usuario REPORTES
GRANT EXECUTE ON SEGSOCIAL.SP_TEST_BACKGROUND_JOB TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_TEST_QUICK_JOB TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_TEST_SLOW_JOB TO REPORTES;

-- 6. SP de limpieza para jobs de prueba
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_CLEANUP_TEST_JOBS AS
BEGIN
  DELETE FROM SEGSOCIAL.JOB_PROGRESS 
  WHERE JOB_ID LIKE 'TEST_%' 
     OR LAST_UPDATE < SYSTIMESTAMP - INTERVAL '2' HOUR;
  COMMIT;
END;
/

GRANT EXECUTE ON SEGSOCIAL.SP_CLEANUP_TEST_JOBS TO REPORTES;

-- NOTAS DE COMPATIBILIDAD:
-- - Este script usa CONNECT BY LEVEL en lugar de DBMS_LOCK.SLEEP
-- - Compatible con Oracle 11g que no tiene DBMS_LOCK disponible por defecto
-- - Si tienes permisos para DBMS_LOCK, puedes reemplazar el bloque de simulación por:
--   DBMS_LOCK.SLEEP(v_step_duration);

-- Comentarios de uso:
-- Para probar manualmente:
-- DECLARE
--   v_result VARCHAR2(200);
-- BEGIN
--   SEGSOCIAL.SP_TEST_QUICK_JOB('TEST_MANUAL_001', v_result);
--   DBMS_OUTPUT.PUT_LINE('Resultado: ' || v_result);
-- END;
-- /

-- Para verificar progreso durante ejecución:
-- SELECT * FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = 'TEST_MANUAL_001';