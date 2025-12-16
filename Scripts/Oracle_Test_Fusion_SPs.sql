-- =====================================================
-- SP DE PRUEBA para Background Jobs
-- Simula el proceso PROCESAR_PERIODO con reporte de progreso
-- =====================================================

-- =====================================================
-- 1. STORED PROCEDURE DE PRUEBA PRINCIPAL
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_PROCESAR_PERIODO(
    p_periodo IN DATE,
    p_job_id IN VARCHAR2 DEFAULT NULL,
    p_duration_seconds IN NUMBER DEFAULT 60  -- Duración total del test
) AS
    v_steps CONSTANT NUMBER := 10;-- Número de pasos
    v_step_duration NUMBER;
  v_current_progress NUMBER := 0;
    v_registros_procesados NUMBER := 0;
    v_total_registros NUMBER := 1000; -- Simular 1000 registros
    
    -- Procedure interno para actualizar progreso
    PROCEDURE actualizar_progreso(p_porcentaje IN NUMBER, p_mensaje IN VARCHAR2) IS
 PRAGMA AUTONOMOUS_TRANSACTION;
    BEGIN
     IF p_job_id IS NOT NULL THEN
       MERGE INTO SEGSOCIAL.JOB_PROGRESS t
   USING (SELECT p_job_id AS JOB_ID FROM dual) s
         ON (t.JOB_ID = s.JOB_ID)
 WHEN MATCHED THEN 
       UPDATE SET 
 t.PROGRESS_PCT = p_porcentaje,
 t.STATUS_MESSAGE = p_mensaje,
    t.LAST_UPDATE = SYSTIMESTAMP
   WHEN NOT MATCHED THEN 
    INSERT (JOB_ID, PROGRESS_PCT, STATUS_MESSAGE, LAST_UPDATE)
          VALUES (p_job_id, p_porcentaje, p_mensaje, SYSTIMESTAMP);
 COMMIT;
        END IF;
    EXCEPTION
  WHEN OTHERS THEN
          NULL; -- Si falla, continuar
    END actualizar_progreso;
    
    -- Procedure optimizado para simular trabajo en Oracle 11g
    -- Usa un loop simple en lugar de CONNECT BY LEVEL
    PROCEDURE simular_trabajo(p_segundos IN NUMBER) IS
   v_inicio TIMESTAMP := SYSTIMESTAMP;
        v_objetivo TIMESTAMP;
    v_contador NUMBER := 0;
        v_dummy NUMBER := 0;
    BEGIN
        -- Calcular el timestamp objetivo
     v_objetivo := v_inicio + NUMTODSINTERVAL(p_segundos, 'SECOND');
        
        -- Loop que consume CPU hasta alcanzar el tiempo deseado
        WHILE SYSTIMESTAMP < v_objetivo LOOP
    -- Operaciones que consumen CPU sin usar DBMS_LOCK
            FOR i IN 1..1000 LOOP
   v_dummy := v_dummy + SQRT(i) * 0.0001;
    v_contador := v_contador + 1;
            END LOOP;
            
            -- Pequeña pausa cada 10000 iteraciones para no saturar
 IF MOD(v_contador, 10000) = 0 THEN
          v_dummy := DBMS_RANDOM.VALUE(1, 100);
      END IF;
    END LOOP;
    END simular_trabajo;
    
BEGIN
    v_step_duration := p_duration_seconds / v_steps;
    
    -- ============================================================
 -- PASO 0: Inicialización (0%)
    -- ============================================================
 actualizar_progreso(0, 'Inicializando proceso de prueba para periodo ' || TO_CHAR(p_periodo, 'YYYY-MM'));
  
    -- ============================================================
    -- PASO 1: Validaciones (0-10%)
    -- ============================================================
  actualizar_progreso(5, 'Validando periodo ' || TO_CHAR(p_periodo, 'YYYY-MM') || '...');
    simular_trabajo(v_step_duration);
    
    -- Simular validación
  IF p_periodo IS NULL THEN
      actualizar_progreso(-1, 'ERROR: Periodo no puede ser nulo');
        RAISE_APPLICATION_ERROR(-20001, 'Periodo no puede ser nulo');
    END IF;
    
    actualizar_progreso(10, 'Validaciones completadas');
    simular_trabajo(v_step_duration * 0.5);
    
    -- ============================================================
    -- PASO 2: Carga de datos (10-30%)
    -- ============================================================
    actualizar_progreso(12, 'Cargando datos del periodo...');
    simular_trabajo(v_step_duration);
    
    actualizar_progreso(20, 'Preparando datos para procesamiento...');
    simular_trabajo(v_step_duration);
    
    actualizar_progreso(30, 'Carga completada. Total de registros: ' || v_total_registros);
    simular_trabajo(v_step_duration * 0.5);
    
    -- ============================================================
    -- PASO 3: Procesamiento en lotes (30-80%)
    -- ============================================================
    actualizar_progreso(35, 'Iniciando procesamiento de registros...');
    simular_trabajo(v_step_duration * 0.5);
    
    -- Simular procesamiento en 5 lotes
    FOR i IN 1..5 LOOP
   v_registros_procesados := v_registros_procesados + (v_total_registros / 5);
     
      actualizar_progreso(
    30 + ROUND((i / 5) * 50),
      'Procesando lote ' || i || ' de 5: ' || 
v_registros_procesados || ' de ' || v_total_registros || ' registros'
  );
        
        simular_trabajo(v_step_duration);
    END LOOP;
    
    actualizar_progreso(80, 'Procesamiento completado. ' || v_registros_procesados || ' registros procesados');
    simular_trabajo(v_step_duration * 0.5);
    
    -- ============================================================
    -- PASO 4: Validaciones finales (80-90%)
    -- ============================================================
    actualizar_progreso(85, 'Ejecutando validaciones finales...');
    simular_trabajo(v_step_duration);
    
    actualizar_progreso(90, 'Validaciones finales completadas');
  simular_trabajo(v_step_duration * 0.5);
    
-- ============================================================
    -- PASO 5: Actualización de estadísticas (90-95%)
    -- ============================================================
    actualizar_progreso(92, 'Actualizando estadísticas...');
    simular_trabajo(v_step_duration * 0.5);
    
    actualizar_progreso(95, 'Estadísticas actualizadas');
    simular_trabajo(v_step_duration * 0.5);
    
  -- ============================================================
    -- PASO 6: Finalización (95-100%)
    -- ============================================================
    actualizar_progreso(98, 'Limpiando datos temporales...');
    simular_trabajo(v_step_duration * 0.5);
  
  actualizar_progreso(100, 
        'Proceso completado exitosamente para periodo ' || TO_CHAR(p_periodo, 'YYYY-MM') ||
  '. ' || v_registros_procesados || ' registros procesados.');
    
EXCEPTION
    WHEN OTHERS THEN
      actualizar_progreso(-1, 'ERROR: ' || SQLERRM);
     RAISE;
END SP_TEST_PROCESAR_PERIODO;
/

-- =====================================================
-- 2. WRAPPER CORTO (30 segundos)
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_FUSION_QUICK(
  p_periodo IN DATE,
    p_job_id IN VARCHAR2 DEFAULT NULL
) AS
BEGIN
    SEGSOCIAL.SP_TEST_PROCESAR_PERIODO(
        p_periodo => p_periodo,
      p_job_id => p_job_id,
    p_duration_seconds => 30
    );
END SP_TEST_FUSION_QUICK;
/

-- =====================================================
-- 3. WRAPPER LARGO (2 minutos)
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_FUSION_SLOW(
 p_periodo IN DATE,
    p_job_id IN VARCHAR2 DEFAULT NULL
) AS
BEGIN
    SEGSOCIAL.SP_TEST_PROCESAR_PERIODO(
      p_periodo => p_periodo,
  p_job_id => p_job_id,
        p_duration_seconds => 120
    );
END SP_TEST_FUSION_SLOW;
/

-- =====================================================
-- 4. WRAPPER MUY LARGO (5 minutos) - Para probar timeouts
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_FUSION_VERY_SLOW(
    p_periodo IN DATE,
 p_job_id IN VARCHAR2 DEFAULT NULL
) AS
BEGIN
    SEGSOCIAL.SP_TEST_PROCESAR_PERIODO(
        p_periodo => p_periodo,
        p_job_id => p_job_id,
        p_duration_seconds => 300
    );
END SP_TEST_FUSION_VERY_SLOW;
/

-- =====================================================
-- 5. SP QUE FALLA A PROPÓSITO (Para probar manejo de errores)
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_TEST_FUSION_ERROR(
    p_periodo IN DATE,
    p_job_id IN VARCHAR2 DEFAULT NULL
) AS
    PROCEDURE actualizar_progreso(p_porcentaje IN NUMBER, p_mensaje IN VARCHAR2) IS
     PRAGMA AUTONOMOUS_TRANSACTION;
    BEGIN
        IF p_job_id IS NOT NULL THEN
      MERGE INTO SEGSOCIAL.JOB_PROGRESS t
   USING (SELECT p_job_id AS JOB_ID FROM dual) s
            ON (t.JOB_ID = s.JOB_ID)
     WHEN MATCHED THEN 
   UPDATE SET 
 t.PROGRESS_PCT = p_porcentaje,
t.STATUS_MESSAGE = p_mensaje,
t.LAST_UPDATE = SYSTIMESTAMP
         WHEN NOT MATCHED THEN 
      INSERT (JOB_ID, PROGRESS_PCT, STATUS_MESSAGE, LAST_UPDATE)
         VALUES (p_job_id, p_porcentaje, p_mensaje, SYSTIMESTAMP);
    COMMIT;
        END IF;
    END actualizar_progreso;
    
    PROCEDURE simular_trabajo(p_segundos IN NUMBER) IS
        v_inicio TIMESTAMP := SYSTIMESTAMP;
        v_objetivo TIMESTAMP;
    v_contador NUMBER := 0;
   v_dummy NUMBER := 0;
    BEGIN
        v_objetivo := v_inicio + NUMTODSINTERVAL(p_segundos, 'SECOND');
        WHILE SYSTIMESTAMP < v_objetivo LOOP
     FOR i IN 1..1000 LOOP
           v_dummy := v_dummy + SQRT(i) * 0.0001;
     v_contador := v_contador + 1;
            END LOOP;
      IF MOD(v_contador, 10000) = 0 THEN
   v_dummy := DBMS_RANDOM.VALUE(1, 100);
    END IF;
        END LOOP;
    END simular_trabajo;
    
BEGIN
    actualizar_progreso(0, 'Iniciando proceso que va a fallar...');
  simular_trabajo(1);
    actualizar_progreso(25, 'Procesando...');
  simular_trabajo(1);
    actualizar_progreso(50, 'A punto de fallar...');
  simular_trabajo(1);
    
    -- Simular error
    RAISE_APPLICATION_ERROR(-20099, 'Error simulado para testing de manejo de errores');
    
EXCEPTION
    WHEN OTHERS THEN
   actualizar_progreso(-1, 'ERROR: ' || SQLERRM);
      RAISE;
END SP_TEST_FUSION_ERROR;
/

-- =====================================================
-- 6. GRANTS
-- =====================================================

GRANT EXECUTE ON SEGSOCIAL.SP_TEST_PROCESAR_PERIODO TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_TEST_FUSION_QUICK TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_TEST_FUSION_SLOW TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_TEST_FUSION_VERY_SLOW TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_TEST_FUSION_ERROR TO REPORTES;

-- =====================================================
-- 7. TESTING MANUAL
-- =====================================================

-- Test 1: Ejecución sin job_id (modo síncrono simulado)
BEGIN
    SEGSOCIAL.SP_TEST_FUSION_QUICK(
p_periodo => TO_DATE('2024-01-01', 'YYYY-MM-DD'),
        p_job_id => NULL
    );
    DBMS_OUTPUT.PUT_LINE('Test completado sin tracking');
END;
/

-- Test 2: Ejecución con job_id (modo async con tracking)
DECLARE
    v_job_id VARCHAR2(50) := 'TEST_MANUAL_' || TO_CHAR(SYSTIMESTAMP, 'YYYYMMDDHH24MISS');
BEGIN
    DBMS_OUTPUT.PUT_LINE('Job ID: ' || v_job_id);
    
    SEGSOCIAL.SP_TEST_FUSION_QUICK(
    p_periodo => TO_DATE('2024-01-01', 'YYYY-MM-DD'),
        p_job_id => v_job_id
    );
    
    DBMS_OUTPUT.PUT_LINE('Test completado');
    
    -- Ver resultado final
    FOR rec IN (SELECT * FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = v_job_id) LOOP
        DBMS_OUTPUT.PUT_LINE('Progreso final: ' || rec.PROGRESS_PCT || '% - ' || rec.STATUS_MESSAGE);
    END LOOP;
END;
/

-- Test 3: Monitorear durante ejecución (ejecutar en otra sesión)
SELECT 
    JOB_ID,
    PROGRESS_PCT || '%' AS PROGRESO,
    STATUS_MESSAGE,
    TO_CHAR(LAST_UPDATE, 'HH24:MI:SS') AS HORA,
    ROUND(EXTRACT(SECOND FROM (SYSTIMESTAMP - LAST_UPDATE)), 0) AS SEGUNDOS_DESDE_UPDATE
FROM SEGSOCIAL.JOB_PROGRESS
WHERE JOB_ID LIKE 'TEST_%'
ORDER BY LAST_UPDATE DESC;

-- Test 4: Probar SP que falla
DECLARE
    v_job_id VARCHAR2(50) := 'TEST_ERROR_' || TO_CHAR(SYSTIMESTAMP, 'YYYYMMDDHH24MISS');
BEGIN
    DBMS_OUTPUT.PUT_LINE('Job ID (error test): ' || v_job_id);
    
    BEGIN
  SEGSOCIAL.SP_TEST_FUSION_ERROR(
            p_periodo => TO_DATE('2024-01-01', 'YYYY-MM-DD'),
        p_job_id => v_job_id
        );
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Error capturado (esperado): ' || SQLERRM);
    END;
    
    -- Ver registro de error
    FOR rec IN (SELECT * FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = v_job_id) LOOP
   DBMS_OUTPUT.PUT_LINE('Estado error: ' || rec.PROGRESS_PCT || '% - ' || rec.STATUS_MESSAGE);
    END LOOP;
END;
/

-- Limpiar registros de prueba
DELETE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID LIKE 'TEST_%';
COMMIT;

-- =====================================================
-- 8. QUERIES DE MONITOREO
-- =====================================================

-- Ver todos los jobs de prueba activos
SELECT 
    JOB_ID,
    PROGRESS_PCT,
    STATUS_MESSAGE,
    TO_CHAR(LAST_UPDATE, 'DD/MM/YYYY HH24:MI:SS') AS ULTIMA_ACTUALIZACION,
 ROUND(EXTRACT(SECOND FROM (SYSTIMESTAMP - LAST_UPDATE)) / 60, 2) AS MINUTOS_DESDE_UPDATE,
    CASE 
        WHEN PROGRESS_PCT >= 100 THEN 'COMPLETADO'
        WHEN PROGRESS_PCT < 0 THEN 'ERROR'
     WHEN PROGRESS_PCT = 0 THEN 'INICIANDO'
     ELSE 'EN PROGRESO'
    END AS ESTADO
FROM SEGSOCIAL.JOB_PROGRESS
WHERE JOB_ID LIKE 'TEST_%'
ORDER BY LAST_UPDATE DESC;

-- Ver historial de progreso de un job específico
-- (Nota: necesitarías una tabla de auditoría para esto, por ahora solo muestra el estado actual)
SELECT 
    JOB_ID,
    PROGRESS_PCT || '%' AS PROGRESO,
  STATUS_MESSAGE,
    LAST_UPDATE
FROM SEGSOCIAL.JOB_PROGRESS
WHERE JOB_ID = 'TU_JOB_ID_AQUI';

-- =====================================================
-- NOTAS DE USO
-- =====================================================

/*
VARIANTES DISPONIBLES:

1. SP_TEST_FUSION_QUICK (30 segundos)
   - Ideal para testing rápido
   - 10 pasos con actualización de progreso
   - Uso: Testing de desarrollo

2. SP_TEST_FUSION_SLOW (2 minutos)
   - Para testing de procesos medianos
 - Útil para verificar polling del frontend
   - Uso: Testing de integración

3. SP_TEST_FUSION_VERY_SLOW (5 minutos)
   - Para probar timeouts
   - Verificar que el sistema maneja procesos largos
 - Uso: Testing de performance y timeouts

4. SP_TEST_FUSION_ERROR
   - Falla intencionalmente al 50%
   - Verifica manejo de errores
   - Uso: Testing de error handling

EJEMPLOS DE USO DESDE .NET:

// En tu código de prueba
var request = new FusionDatosRequest 
{ 
    Periodo = new DateTime(2024, 1, 1) 
};

// Para testing rápido (30s), cambiar en IJobProgressRepository:
await _jobProgressRepository.ExecuteTestFusionQuickAsync(periodo, jobId);

// Para testing lento (2 min):
await _jobProgressRepository.ExecuteTestFusionSlowAsync(periodo, jobId);

// Para testing de errores:
await _jobProgressRepository.ExecuteTestFusionErrorAsync(periodo, jobId);

PASOS DE PROGRESO:
- 0%: Inicialización
- 5-10%: Validaciones iniciales
- 10-30%: Carga de datos
- 30-80%: Procesamiento en lotes (5 lotes)
- 80-90%: Validaciones finales
- 90-95%: Actualización de estadísticas
- 95-100%: Limpieza y finalización

ESTADOS DE ERROR:
- PROGRESS_PCT = -1: Indica error
- STATUS_MESSAGE contiene SQLERRM

LIMPIEZA:
DELETE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID LIKE 'TEST_%';
*/
