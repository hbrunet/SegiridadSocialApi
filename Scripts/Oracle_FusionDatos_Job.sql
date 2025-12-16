-- =====================================================
-- SP: Proceso de Fusión de Datos con Reporte de Progreso
-- Propósito: Fusionar datos de múltiples fuentes/tablas
--        reportando progreso en tiempo real a la API .NET
-- Autor: Sistema
-- Fecha: 2025-01-XX
-- =====================================================

-- =====================================================
-- 1. STORED PROCEDURE PRINCIPAL DE FUSIÓN
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_FUSION_DATOS(
  p_job_id IN VARCHAR2,
  p_id_archivo IN NUMBER,
  p_tipo_fusion IN VARCHAR2 DEFAULT 'COMPLETA',  -- COMPLETA, INCREMENTAL, VALIDACION
  p_fecha_desde IN DATE DEFAULT NULL,
  p_fecha_hasta IN DATE DEFAULT NULL,
  p_resultado OUT VARCHAR2
) AS
  -- Variables de control
  v_total_registros NUMBER := 0;
  v_registros_procesados NUMBER := 0;
  v_registros_fusionados NUMBER := 0;
  v_registros_errores NUMBER := 0;
  v_progreso_actual NUMBER := 0;
  v_paso_actual VARCHAR2(500);
  
  -- Variables de resultado
  v_inicio TIMESTAMP := SYSTIMESTAMP;
  v_fin TIMESTAMP;
  v_duracion_segundos NUMBER;
  
  -- Constantes para progreso
  c_progreso_validacion CONSTANT NUMBER := 20;
  c_progreso_preparacion CONSTANT NUMBER := 40;
  c_progreso_fusion CONSTANT NUMBER := 80;
  c_progreso_finalizacion CONSTANT NUMBER := 100;
  
  -- Procedure interno para actualizar progreso
  PROCEDURE actualizar_progreso(p_porcentaje IN NUMBER, p_mensaje IN VARCHAR2) IS
    PRAGMA AUTONOMOUS_TRANSACTION;
  BEGIN
    UPDATE SEGSOCIAL.JOB_PROGRESS 
    SET PROGRESS_PCT = p_porcentaje,
        STATUS_MESSAGE = p_mensaje,
        LAST_UPDATE = SYSTIMESTAMP
    WHERE JOB_ID = p_job_id;
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      -- Si falla el update, continuar sin romper el proceso principal
      NULL;
  END actualizar_progreso;
  
BEGIN
  -- ============================================================
  -- PASO 0: Inicialización (0%)
  -- ============================================================
  actualizar_progreso(0, 'Inicializando proceso de fusión de datos...');
  
  -- Registrar inicio del proceso
  INSERT INTO SEGSOCIAL.LOG_FUSION_DATOS (
    JOB_ID, ID_ARCHIVO, TIPO_FUSION, FECHA_INICIO, ESTADO
  ) VALUES (
    p_job_id, p_id_archivo, p_tipo_fusion, SYSTIMESTAMP, 'INICIADO'
  );
  COMMIT;
  
  -- ============================================================
  -- PASO 1: Validaciones Iniciales (5%)
  -- ============================================================
  actualizar_progreso(5, 'Validando parámetros de entrada...');
  
  -- Validar que existe el archivo
  SELECT COUNT(*) INTO v_total_registros
  FROM USUARIO.TMP_NOV_DDJJ_PREV
  WHERE IDARCHIVO = p_id_archivo;
  
  IF v_total_registros = 0 THEN
    actualizar_progreso(-1, 'ERROR: No se encontraron registros para el archivo ' || p_id_archivo);
    RAISE_APPLICATION_ERROR(-20001, 'No existen registros para fusionar del archivo ' || p_id_archivo);
  END IF;
  
  actualizar_progreso(10, 'Validación completada. Total de registros: ' || v_total_registros);
  
  -- ============================================================
  -- PASO 2: Validación de Datos (10% - 20%)
  -- ============================================================
  actualizar_progreso(12, 'Validando integridad de datos...');
  
  -- Validar duplicados
  SELECT COUNT(*) INTO v_registros_errores
  FROM (
    SELECT CUIL, COUNT(*) as cant
    FROM USUARIO.TMP_NOV_DDJJ_PREV
    WHERE IDARCHIVO = p_id_archivo
    GROUP BY CUIL
    HAVING COUNT(*) > 1
  );
  
  IF v_registros_errores > 0 THEN
    actualizar_progreso(-1, 'ERROR: Se encontraron ' || v_registros_errores || ' CUILs duplicados');
    RAISE_APPLICATION_ERROR(-20002, 'Existen CUILs duplicados en el archivo');
  END IF;
  
  actualizar_progreso(15, 'Validando campos obligatorios...');
  
  -- Validar campos obligatorios
  SELECT COUNT(*) INTO v_registros_errores
  FROM USUARIO.TMP_NOV_DDJJ_PREV
  WHERE IDARCHIVO = p_id_archivo
    AND (CUIL IS NULL OR APENOM IS NULL);
  
  IF v_registros_errores > 0 THEN
    actualizar_progreso(-1, 'ERROR: ' || v_registros_errores || ' registros con campos obligatorios nulos');
    RAISE_APPLICATION_ERROR(-20003, 'Existen registros con campos obligatorios vacíos');
  END IF;
  
  actualizar_progreso(c_progreso_validacion, 'Validaciones completadas exitosamente');
  
  -- ============================================================
  -- PASO 3: Preparación de Datos para Fusión (20% - 40%)
  -- ============================================================
  actualizar_progreso(25, 'Preparando datos para fusión...');
  
  -- Crear tabla temporal de trabajo si no existe
  BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE SEGSOCIAL.TMP_FUSION_TRABAJO PURGE';
  EXCEPTION
    WHEN OTHERS THEN NULL;
  END;
  
  actualizar_progreso(28, 'Creando tabla de trabajo temporal...');
  
  EXECUTE IMMEDIATE '
    CREATE TABLE SEGSOCIAL.TMP_FUSION_TRABAJO AS
    SELECT 
      t.*,
      SYSTIMESTAMP as FECHA_PROCESO,
  ''' || p_job_id || ''' as JOB_ID
    FROM USUARIO.TMP_NOV_DDJJ_PREV t
    WHERE t.IDARCHIVO = ' || p_id_archivo;
  
  actualizar_progreso(32, 'Enriqueciendo datos con información de reparticiones...');
  
  -- Aquí puedes agregar JOINs con otras tablas para enriquecer los datos
  -- Ejemplo: agregar información de la repartición
  EXECUTE IMMEDIATE '
    UPDATE SEGSOCIAL.TMP_FUSION_TRABAJO t
    SET t.OBSERVACIONES = (
      SELECT r.DESCRIPCION
    FROM SEGSOCIAL.REPARTICION r
      WHERE r.IDREP = t.IDREP
    )';
  
  COMMIT;
  
  actualizar_progreso(c_progreso_preparacion, 'Preparación de datos completada. Registros preparados: ' || v_total_registros);
  
  -- ============================================================
  -- PASO 4: Proceso de Fusión (40% - 80%)
  -- ============================================================
  actualizar_progreso(45, 'Iniciando proceso de fusión de datos...');
  
  -- Procesar en lotes para reportar progreso
  DECLARE
    v_lote_size CONSTANT NUMBER := 1000;
    v_lotes_totales NUMBER;
    v_lote_actual NUMBER := 0;
    v_porcentaje_fusion NUMBER;
    
    CURSOR c_datos_fusion IS
      SELECT *
      FROM SEGSOCIAL.TMP_FUSION_TRABAJO
   ORDER BY ID;
    
    TYPE t_datos_array IS TABLE OF c_datos_fusion%ROWTYPE INDEX BY PLS_INTEGER;
    v_datos_lote t_datos_array;
  BEGIN
    -- Calcular total de lotes
    v_lotes_totales := CEIL(v_total_registros / v_lote_size);
    
    OPEN c_datos_fusion;
    LOOP
      FETCH c_datos_fusion BULK COLLECT INTO v_datos_lote LIMIT v_lote_size;
      EXIT WHEN v_datos_lote.COUNT = 0;
  
      v_lote_actual := v_lote_actual + 1;
      
      -- Procesar el lote actual
      FORALL i IN 1..v_datos_lote.COUNT
      -- Aquí va tu lógica de fusión específica
    -- Ejemplo: INSERT o MERGE en tabla destino
  MERGE INTO SEGSOCIAL.DATOS_FUSIONADOS df
        USING (SELECT 
    v_datos_lote(i).CUIL as CUIL,
   v_datos_lote(i).APENOM as APENOM,
                v_datos_lote(i).REMUNIMPONIBLE1 as REMUN1,
    v_datos_lote(i).REMUNIMPONIBLE2 as REMUN2,
 v_datos_lote(i).REMUNIMPONIBLE3 as REMUN3,
    v_datos_lote(i).FECHA_PROCESO as FECHA_PROCESO,
       p_id_archivo as ID_ARCHIVO,
                p_job_id as JOB_ID
         FROM DUAL) src
        ON (df.CUIL = src.CUIL AND df.ID_ARCHIVO = src.ID_ARCHIVO)
        WHEN MATCHED THEN
       UPDATE SET 
         df.APENOM = src.APENOM,
            df.REMUN1 = src.REMUN1,
            df.REMUN2 = src.REMUN2,
            df.REMUN3 = src.REMUN3,
   df.FECHA_ACTUALIZACION = src.FECHA_PROCESO,
      df.JOB_ID_ACTUALIZACION = src.JOB_ID
 WHEN NOT MATCHED THEN
  INSERT (CUIL, APENOM, REMUN1, REMUN2, REMUN3, FECHA_PROCESO, ID_ARCHIVO, JOB_ID)
        VALUES (src.CUIL, src.APENOM, src.REMUN1, src.REMUN2, src.REMUN3, 
  src.FECHA_PROCESO, src.ID_ARCHIVO, src.JOB_ID);
      
      v_registros_procesados := v_registros_procesados + v_datos_lote.COUNT;
      v_registros_fusionados := v_registros_fusionados + SQL%ROWCOUNT;
      
      COMMIT;
      
      -- Calcular progreso entre 40% y 80%
      v_porcentaje_fusion := c_progreso_preparacion + 
        ((c_progreso_fusion - c_progreso_preparacion) * v_lote_actual / v_lotes_totales);
  
      actualizar_progreso(
        ROUND(v_porcentaje_fusion), 
        'Fusionando datos: ' || v_registros_procesados || ' de ' || v_total_registros || 
        ' registros (' || ROUND((v_registros_procesados/v_total_registros)*100) || '%)'
   );
      
    END LOOP;
    CLOSE c_datos_fusion;
  END;
  
  actualizar_progreso(c_progreso_fusion, 'Fusión completada. Registros fusionados: ' || v_registros_fusionados);
  
  -- ============================================================
  -- PASO 5: Post-Procesamiento y Validaciones Finales (80% - 95%)
  -- ============================================================
  actualizar_progreso(85, 'Ejecutando validaciones post-fusión...');
  
  -- Validar que todos los registros se fusionaron correctamente
  SELECT COUNT(*) INTO v_registros_errores
  FROM SEGSOCIAL.TMP_FUSION_TRABAJO tmp
WHERE NOT EXISTS (
    SELECT 1 FROM SEGSOCIAL.DATOS_FUSIONADOS df
    WHERE df.CUIL = tmp.CUIL 
AND df.ID_ARCHIVO = p_id_archivo
  );
  
  IF v_registros_errores > 0 THEN
    actualizar_progreso(88, 'ADVERTENCIA: ' || v_registros_errores || ' registros no se fusionaron');
  END IF;
  
  actualizar_progreso(90, 'Actualizando estadísticas y metadatos...');
  
  -- Actualizar estadísticas de la tabla fusionada
  DBMS_STATS.GATHER_TABLE_STATS(
    ownname => 'SEGSOCIAL',
    tabname => 'DATOS_FUSIONADOS',
    estimate_percent => DBMS_STATS.AUTO_SAMPLE_SIZE
  );
  
  actualizar_progreso(95, 'Limpiando datos temporales...');
  
  -- Limpiar tabla temporal
  EXECUTE IMMEDIATE 'DROP TABLE SEGSOCIAL.TMP_FUSION_TRABAJO PURGE';
  
  -- ============================================================
  -- PASO 6: Finalización (95% - 100%)
  -- ============================================================
  v_fin := SYSTIMESTAMP;
  v_duracion_segundos := EXTRACT(SECOND FROM (v_fin - v_inicio)) +
                   EXTRACT(MINUTE FROM (v_fin - v_inicio)) * 60 +
    EXTRACT(HOUR FROM (v_fin - v_inicio)) * 3600;
  
  -- Registrar resultado final
  UPDATE SEGSOCIAL.LOG_FUSION_DATOS
  SET FECHA_FIN = v_fin,
      DURACION_SEGUNDOS = v_duracion_segundos,
      REGISTROS_PROCESADOS = v_registros_procesados,
      REGISTROS_FUSIONADOS = v_registros_fusionados,
      REGISTROS_ERRORES = v_registros_errores,
      ESTADO = 'COMPLETADO'
  WHERE JOB_ID = p_job_id;
  COMMIT;
  
  -- Construir mensaje de resultado
  p_resultado := 'FUSION_COMPLETADA|' ||
         'Procesados:' || v_registros_procesados || '|' ||
     'Fusionados:' || v_registros_fusionados || '|' ||
                 'Errores:' || v_registros_errores || '|' ||
          'Duracion:' || ROUND(v_duracion_segundos, 2) || 's';
  
  actualizar_progreso(c_progreso_finalizacion, 'Proceso completado exitosamente en ' || 
    ROUND(v_duracion_segundos, 2) || ' segundos');
  
EXCEPTION
  WHEN OTHERS THEN
    -- Registrar error
    v_fin := SYSTIMESTAMP;
    v_duracion_segundos := EXTRACT(SECOND FROM (v_fin - v_inicio)) +
            EXTRACT(MINUTE FROM (v_fin - v_inicio)) * 60;
    
    UPDATE SEGSOCIAL.LOG_FUSION_DATOS
    SET FECHA_FIN = v_fin,
        DURACION_SEGUNDOS = v_duracion_segundos,
        REGISTROS_PROCESADOS = v_registros_procesados,
        ERROR_MESSAGE = SQLERRM,
    ESTADO = 'ERROR'
    WHERE JOB_ID = p_job_id;
    COMMIT;
    
    -- Actualizar progreso con error
    actualizar_progreso(-1, 'ERROR: ' || SQLERRM);
    
    -- Limpiar recursos
    BEGIN
      EXECUTE IMMEDIATE 'DROP TABLE SEGSOCIAL.TMP_FUSION_TRABAJO PURGE';
    EXCEPTION
      WHEN OTHERS THEN NULL;
    END;
    
    -- Propagar el error
    RAISE;
END SP_FUSION_DATOS;
/

-- =====================================================
-- 2. TABLA DE LOG PARA AUDITORÍA
-- =====================================================

CREATE TABLE SEGSOCIAL.LOG_FUSION_DATOS (
  ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  JOB_ID VARCHAR2(50) NOT NULL,
  ID_ARCHIVO NUMBER NOT NULL,
  TIPO_FUSION VARCHAR2(20),
  FECHA_INICIO TIMESTAMP,
  FECHA_FIN TIMESTAMP,
  DURACION_SEGUNDOS NUMBER(10,2),
  REGISTROS_PROCESADOS NUMBER,
  REGISTROS_FUSIONADOS NUMBER,
  REGISTROS_ERRORES NUMBER,
  ERROR_MESSAGE VARCHAR2(4000),
  ESTADO VARCHAR2(20),
  CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP
);

CREATE INDEX IDX_LOG_FUSION_JOB ON SEGSOCIAL.LOG_FUSION_DATOS(JOB_ID);
CREATE INDEX IDX_LOG_FUSION_ARCHIVO ON SEGSOCIAL.LOG_FUSION_DATOS(ID_ARCHIVO);
CREATE INDEX IDX_LOG_FUSION_FECHA ON SEGSOCIAL.LOG_FUSION_DATOS(FECHA_INICIO);

COMMENT ON TABLE SEGSOCIAL.LOG_FUSION_DATOS IS 'Log de auditoría para procesos de fusión de datos';

-- =====================================================
-- 3. TABLA DESTINO DE EJEMPLO (ajustar según necesidades)
-- =====================================================

CREATE TABLE SEGSOCIAL.DATOS_FUSIONADOS (
  ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  CUIL VARCHAR2(11) NOT NULL,
  APENOM VARCHAR2(500),
  REMUN1 NUMBER(12,2),
  REMUN2 NUMBER(12,2),
  REMUN3 NUMBER(12,2),
  FECHA_PROCESO TIMESTAMP,
  ID_ARCHIVO NUMBER,
  JOB_ID VARCHAR2(50),
  FECHA_ACTUALIZACION TIMESTAMP,
  JOB_ID_ACTUALIZACION VARCHAR2(50),
  CONSTRAINT UK_DATOS_FUSION UNIQUE (CUIL, ID_ARCHIVO)
);

CREATE INDEX IDX_DATOS_FUSION_CUIL ON SEGSOCIAL.DATOS_FUSIONADOS(CUIL);
CREATE INDEX IDX_DATOS_FUSION_ARCHIVO ON SEGSOCIAL.DATOS_FUSIONADOS(ID_ARCHIVO);
CREATE INDEX IDX_DATOS_FUSION_JOB ON SEGSOCIAL.DATOS_FUSIONADOS(JOB_ID);

-- =====================================================
-- 4. GRANTS NECESARIOS
-- =====================================================

GRANT EXECUTE ON SEGSOCIAL.SP_FUSION_DATOS TO REPORTES;
GRANT SELECT, INSERT, UPDATE, DELETE ON SEGSOCIAL.LOG_FUSION_DATOS TO REPORTES;
GRANT SELECT, INSERT, UPDATE, DELETE ON SEGSOCIAL.DATOS_FUSIONADOS TO REPORTES;

-- =====================================================
-- 5. FUNCIÓN AUXILIAR: Consultar Estado del Proceso
-- =====================================================

CREATE OR REPLACE FUNCTION SEGSOCIAL.FN_GET_FUSION_STATUS(
  p_job_id IN VARCHAR2
) RETURN VARCHAR2 AS
  v_resultado VARCHAR2(4000);
  v_progreso NUMBER;
  v_mensaje VARCHAR2(500);
  v_registros_proc NUMBER;
  v_estado VARCHAR2(20);
BEGIN
  -- Obtener progreso actual
  SELECT PROGRESS_PCT, STATUS_MESSAGE
  INTO v_progreso, v_mensaje
  FROM SEGSOCIAL.JOB_PROGRESS
  WHERE JOB_ID = p_job_id;
  
  -- Obtener información del log
  BEGIN
    SELECT 
      REGISTROS_PROCESADOS,
      ESTADO
    INTO v_registros_proc, v_estado
    FROM SEGSOCIAL.LOG_FUSION_DATOS
    WHERE JOB_ID = p_job_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
  v_registros_proc := 0;
      v_estado := 'DESCONOCIDO';
  END;
  
  v_resultado := 'Progreso: ' || v_progreso || '%' ||
       ' | Estado: ' || v_estado ||
         ' | Procesados: ' || v_registros_proc ||
  ' | Mensaje: ' || v_mensaje;
  
  RETURN v_resultado;
  
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RETURN 'Job no encontrado';
  WHEN OTHERS THEN
    RETURN 'Error: ' || SQLERRM;
END FN_GET_FUSION_STATUS;
/

GRANT EXECUTE ON SEGSOCIAL.FN_GET_FUSION_STATUS TO REPORTES;

-- =====================================================
-- 6. PROCEDURE DE LIMPIEZA
-- =====================================================

CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_LIMPIAR_FUSION_ANTIGUOS(
  p_dias_antiguedad IN NUMBER DEFAULT 30
) AS
  v_registros_eliminados NUMBER;
BEGIN
  -- Limpiar logs antiguos
  DELETE FROM SEGSOCIAL.LOG_FUSION_DATOS
  WHERE FECHA_INICIO < SYSTIMESTAMP - INTERVAL '30' DAY;
  
  v_registros_eliminados := SQL%ROWCOUNT;
  COMMIT;
  
  DBMS_OUTPUT.PUT_LINE('Logs de fusión eliminados: ' || v_registros_eliminados);
  
  -- Limpiar progreso antiguo
  DELETE FROM SEGSOCIAL.JOB_PROGRESS
  WHERE LAST_UPDATE < SYSTIMESTAMP - INTERVAL '7' DAY
    AND JOB_ID IN (SELECT JOB_ID FROM SEGSOCIAL.LOG_FUSION_DATOS WHERE ESTADO = 'COMPLETADO');
  
  v_registros_eliminados := SQL%ROWCOUNT;
  COMMIT;
  
  DBMS_OUTPUT.PUT_LINE('Registros de progreso eliminados: ' || v_registros_eliminados);
END SP_LIMPIAR_FUSION_ANTIGUOS;
/

GRANT EXECUTE ON SEGSOCIAL.SP_LIMPIAR_FUSION_ANTIGUOS TO REPORTES;

-- =====================================================
-- 7. EJEMPLOS DE USO
-- =====================================================

-- Ejemplo 1: Ejecutar fusión completa
/*
DECLARE
  v_resultado VARCHAR2(4000);
BEGIN
  SEGSOCIAL.SP_FUSION_DATOS(
    p_job_id => 'TEST_FUSION_001',
p_id_archivo => 12345,
    p_tipo_fusion => 'COMPLETA',
    p_resultado => v_resultado
  );
  
  DBMS_OUTPUT.PUT_LINE('Resultado: ' || v_resultado);
END;
/
*/

-- Ejemplo 2: Consultar estado durante ejecución
/*
SELECT SEGSOCIAL.FN_GET_FUSION_STATUS('TEST_FUSION_001') FROM DUAL;
*/

-- Ejemplo 3: Ver historial de fusiones
/*
SELECT 
  JOB_ID,
  ID_ARCHIVO,
  TIPO_FUSION,
  FECHA_INICIO,
  DURACION_SEGUNDOS,
  REGISTROS_PROCESADOS,
  REGISTROS_FUSIONADOS,
  ESTADO
FROM SEGSOCIAL.LOG_FUSION_DATOS
ORDER BY FECHA_INICIO DESC
FETCH FIRST 20 ROWS ONLY;
*/

-- Ejemplo 4: Ver progreso actual de jobs activos
/*
SELECT 
  jp.JOB_ID,
  jp.PROGRESS_PCT,
  jp.STATUS_MESSAGE,
  lf.REGISTROS_PROCESADOS,
  lf.ESTADO,
  ROUND(EXTRACT(SECOND FROM (SYSTIMESTAMP - lf.FECHA_INICIO)) / 60, 2) AS MINUTOS_TRANSCURRIDOS
FROM SEGSOCIAL.JOB_PROGRESS jp
LEFT JOIN SEGSOCIAL.LOG_FUSION_DATOS lf ON jp.JOB_ID = lf.JOB_ID
WHERE lf.ESTADO IN ('INICIADO', 'EN_PROCESO')
  AND jp.LAST_UPDATE > SYSTIMESTAMP - INTERVAL '1' HOUR;
*/
