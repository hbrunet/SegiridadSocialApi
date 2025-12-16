-- =====================================================
-- Ejemplo de Modificación del SP PROCESAR_PERIODO
-- Para Reportar Progreso a la Tabla JOB_PROGRESS
-- =====================================================

-- NOTA: Este es un EJEMPLO de cómo modificar tu SP existente.
-- Debes adaptar los pasos y porcentajes según tu lógica real.

CREATE OR REPLACE PACKAGE BODY SEGSOCIAL.PKG_DDJJ_FUSION AS

    PROCEDURE PROCESAR_PERIODO (
        p_periodo IN SEGSOCIAL.DDJJ_REP.PERIODO%TYPE,
        p_job_id IN VARCHAR2 DEFAULT NULL
    ) IS
        -- Variables de tu SP (las que ya tienes)
        v_registros_procesados NUMBER := 0;
        v_inicio TIMESTAMP := SYSTIMESTAMP;
        -- ... más variables ...

      -- Procedure interno para actualizar progreso
        PROCEDURE actualizar_progreso(p_porcentaje IN NUMBER, p_mensaje IN VARCHAR2) IS
        PRAGMA AUTONOMOUS_TRANSACTION;
        BEGIN
            -- Solo actualizar si se pasó un job_id
            IF p_job_id IS NOT NULL THEN
                UPDATE SEGSOCIAL.JOB_PROGRESS
 SET PROGRESS_PCT = p_porcentaje,
         STATUS_MESSAGE = p_mensaje,
       LAST_UPDATE = SYSTIMESTAMP
         WHERE JOB_ID = p_job_id;
      COMMIT; -- IMPORTANTE: commit para que la API vea el cambio
      END IF;
  EXCEPTION
 WHEN OTHERS THEN
            -- Si falla el update de progreso, continuar sin romper el proceso
    NULL;
        END actualizar_progreso;

    BEGIN
        -- ============================================================
        -- PASO 0: Inicialización (0%)
        -- ============================================================
        actualizar_progreso(0, 'Inicializando proceso para periodo ' || TO_CHAR(p_periodo, 'YYYY-MM'));

        -- ============================================================
        -- PASO 1: Validaciones Iniciales (5-10%)
 -- ============================================================
     actualizar_progreso(5, 'Validando periodo ' || TO_CHAR(p_periodo, 'YYYY-MM'));

  -- TU LÓGICA DE VALIDACIÓN AQUÍ
        -- Ejemplo:
        -- IF NOT validar_periodo(p_periodo) THEN
      --     RAISE_APPLICATION_ERROR(-20001, 'Periodo inválido');
        -- END IF;

     actualizar_progreso(10, 'Validaciones completadas');

    -- ============================================================
        -- PASO 2: Carga de Datos (10-30%)
        -- ============================================================
  actualizar_progreso(12, 'Cargando datos del periodo...');

  -- TU LÓGICA DE CARGA AQUÍ
        -- Ejemplo:
        -- INSERT INTO tabla_temporal
        -- SELECT ... FROM origen WHERE periodo = p_periodo;
 
        actualizar_progreso(20, 'Datos cargados, preparando procesamiento...');

        -- Si cargas en lotes, actualizar progreso proporcionalmente
        -- FOR i IN 1..total_lotes LOOP
        --     -- cargar lote
     --     actualizar_progreso(
        --         20 + ROUND((i / total_lotes) * 10),
        --         'Cargando lote ' || i || ' de ' || total_lotes
     --     );
        -- END LOOP;

        actualizar_progreso(30, 'Carga completada');

     -- ============================================================
        -- PASO 3: Procesamiento Principal (30-80%)
        -- ============================================================
     actualizar_progreso(35, 'Iniciando procesamiento de registros...');

        -- TU LÓGICA PRINCIPAL AQUÍ
        -- Si procesas en lotes, reportar progreso proporcional
        /*
      DECLARE
    v_total_registros NUMBER;
 v_lote_size CONSTANT NUMBER := 1000;
        v_lotes_procesados NUMBER := 0;
            v_total_lotes NUMBER;
            
     CURSOR c_registros IS
SELECT * FROM tu_tabla WHERE periodo = p_periodo;
        
       TYPE t_registros IS TABLE OF c_registros%ROWTYPE;
v_lote t_registros;
        BEGIN
            SELECT COUNT(*) INTO v_total_registros FROM tu_tabla WHERE periodo = p_periodo;
            v_total_lotes := CEIL(v_total_registros / v_lote_size);
          
          OPEN c_registros;
      LOOP
       FETCH c_registros BULK COLLECT INTO v_lote LIMIT v_lote_size;
         EXIT WHEN v_lote.COUNT = 0;
         
      -- Procesar el lote
  FORALL i IN 1..v_lote.COUNT
           -- Tu lógica de procesamiento aquí
           INSERT INTO tabla_destino VALUES v_lote(i);
           
       v_registros_procesados := v_registros_procesados + v_lote.COUNT;
    v_lotes_procesados := v_lotes_procesados + 1;
     
      COMMIT;
        
         -- Actualizar progreso (30% a 80%)
         actualizar_progreso(
         30 + ROUND((v_lotes_procesados / v_total_lotes) * 50),
  'Procesando: ' || v_registros_procesados || ' de ' || v_total_registros || ' registros'
                );
            END LOOP;
    CLOSE c_registros;
        END;
        */

        actualizar_progreso(80, 'Procesamiento completado, ' || v_registros_procesados || ' registros procesados');

    -- ============================================================
      -- PASO 4: Validaciones Post-Procesamiento (80-90%)
        -- ============================================================
        actualizar_progreso(85, 'Ejecutando validaciones finales...');

        -- TU LÓGICA DE VALIDACIÓN POST-PROCESAMIENTO
        -- Ejemplo:
        -- validar_integridad_referencial();
   -- validar_totales();

      actualizar_progreso(90, 'Validaciones completadas');

        -- ============================================================
        -- PASO 5: Actualización de Estadísticas (90-95%)
    -- ============================================================
        actualizar_progreso(92, 'Actualizando estadísticas...');

        -- Actualizar estadísticas de tablas involucradas
        DBMS_STATS.GATHER_TABLE_STATS(
         ownname => 'SEGSOCIAL',
         tabname => 'TU_TABLA_PRINCIPAL',
         estimate_percent => DBMS_STATS.AUTO_SAMPLE_SIZE
        );

        actualizar_progreso(95, 'Estadísticas actualizadas');

  -- ============================================================
        -- PASO 6: Finalización (95-100%)
     -- ============================================================
    actualizar_progreso(98, 'Limpiando datos temporales...');

        -- TU LÓGICA DE LIMPIEZA
        -- DELETE FROM tabla_temporal;
        -- COMMIT;

        actualizar_progreso(100, 'Proceso completado exitosamente para periodo ' || TO_CHAR(p_periodo, 'YYYY-MM'));

        -- Log final (si tienes tabla de auditoría)
        -- INSERT INTO log_fusion VALUES (
    --     p_periodo,
        --     v_registros_procesados,
        --   SYSTIMESTAMP - v_inicio,
        --     'COMPLETADO'
        -- );
        -- COMMIT;

    EXCEPTION
        WHEN OTHERS THEN
 -- En caso de error, registrar en progreso
        actualizar_progreso(-1, 'ERROR: ' || SQLERRM);

      -- Tu lógica de manejo de errores
      -- ROLLBACK;
-- log_error(p_periodo, SQLERRM);
            
 -- Propagar el error
            RAISE;
    END PROCESAR_PERIODO;

END PKG_DDJJ_FUSION;
/

-- =====================================================
-- ALTERNATIVA: Si prefieres no modificar el SP original
-- puedes crear un wrapper procedure
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.PROCESAR_PERIODO_MONITOREADO (
    p_periodo IN SEGSOCIAL.DDJJ_REP.PERIODO%TYPE,
    p_job_id IN VARCHAR2 DEFAULT NULL
) AS
    PROCEDURE actualizar_progreso(p_porcentaje IN NUMBER, p_mensaje IN VARCHAR2) IS
        PRAGMA AUTONOMOUS_TRANSACTION;
    BEGIN
        IF p_job_id IS NOT NULL THEN
            UPDATE SEGSOCIAL.JOB_PROGRESS
       SET PROGRESS_PCT = p_porcentaje,
    STATUS_MESSAGE = p_mensaje,
   LAST_UPDATE = SYSTIMESTAMP
            WHERE JOB_ID = p_job_id;
     COMMIT;
        END IF;
    END;
BEGIN
    actualizar_progreso(0, 'Iniciando proceso...');
    
    -- Llamar al SP original
    SEGSOCIAL.PKG_DDJJ_FUSION.PROCESAR_PERIODO(p_periodo, NULL);
    
    actualizar_progreso(100, 'Proceso completado');
    
EXCEPTION
    WHEN OTHERS THEN
        actualizar_progreso(-1, 'ERROR: ' || SQLERRM);
RAISE;
END;
/

-- =====================================================
-- Grants necesarios
-- =====================================================

GRANT EXECUTE ON SEGSOCIAL.PKG_DDJJ_FUSION TO REPORTES;
-- Si creaste el wrapper:
-- GRANT EXECUTE ON SEGSOCIAL.PROCESAR_PERIODO_MONITOREADO TO REPORTES;

-- =====================================================
-- Testing Manual
-- =====================================================

-- Test sin job_id (modo síncrono)
BEGIN
    SEGSOCIAL.PKG_DDJJ_FUSION.PROCESAR_PERIODO(
        p_periodo => TO_DATE('2024-01-01', 'YYYY-MM-DD'),
     p_job_id => NULL
    );
 DBMS_OUTPUT.PUT_LINE('Proceso completado sin tracking');
END;
/

-- Test con job_id (modo async con tracking)
DECLARE
    v_job_id VARCHAR2(50) := 'TEST_' || TO_CHAR(SYSTIMESTAMP, 'YYYYMMDDHH24MISS');
BEGIN
    -- Inicializar job progress
  INSERT INTO SEGSOCIAL.JOB_PROGRESS (JOB_ID, PROGRESS_PCT, STATUS_MESSAGE)
    VALUES (v_job_id, 0, 'Test iniciado...');
    COMMIT;
    
    -- Ejecutar con tracking
    SEGSOCIAL.PKG_DDJJ_FUSION.PROCESAR_PERIODO(
        p_periodo => TO_DATE('2024-01-01', 'YYYY-MM-DD'),
        p_job_id => v_job_id
    );
    
    DBMS_OUTPUT.PUT_LINE('Proceso completado con job_id: ' || v_job_id);
    
    -- Ver resultado
    FOR rec IN (SELECT * FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = v_job_id) LOOP
        DBMS_OUTPUT.PUT_LINE('Progreso final: ' || rec.PROGRESS_PCT || '% - ' || rec.STATUS_MESSAGE);
    END LOOP;
END;
/

-- Monitorear durante ejecución (ejecutar en otra sesión)
SELECT 
    JOB_ID,
    PROGRESS_PCT || '%' AS PROGRESO,
    STATUS_MESSAGE,
    TO_CHAR(LAST_UPDATE, 'HH24:MI:SS') AS HORA
FROM SEGSOCIAL.JOB_PROGRESS
WHERE JOB_ID LIKE 'TEST_%'
ORDER BY LAST_UPDATE DESC;

-- Limpiar después de testing
DELETE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID LIKE 'TEST_%';
COMMIT;

-- =====================================================
-- NOTAS IMPORTANTES
-- =====================================================

/*
1. AUTONOMOUS_TRANSACTION:
   - Necesario para que los UPDATEs de progreso se commiteen independientemente
   - Permite ver el progreso sin afectar la transacción principal

2. COMMIT después de cada UPDATE:
   - Sin esto, la API no verá los cambios de progreso hasta que el SP complete
   - Es seguro porque es en transacción autónoma

3. Manejo de errores:
   - Usar PROGRESS_PCT = -1 para indicar error
   - Registrar SQLERRM en STATUS_MESSAGE
   - Propagar el error con RAISE

4. Porcentajes recomendados:
   - 0-10%: Inicialización y validaciones
   - 10-30%: Carga de datos
   - 30-80%: Procesamiento principal
   - 80-95%: Post-procesamiento
   - 95-100%: Limpieza y finalización

5. Si no puedes modificar el SP original:
 - Usar el wrapper PROCESAR_PERIODO_MONITOREADO
   - Actualizar progreso solo al inicio y fin
   - Menos granularidad pero funciona igual

6. Testing:
   - Siempre probar primero sin job_id
   - Luego probar con job_id y monitorear en otra sesión
   - Verificar que los COMMITs funcionan correctamente
*/
