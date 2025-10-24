-- =====================================================
-- Script: Tabla de progreso para Background Jobs
-- Propósito: Permite que los SPs de Oracle reporten su progreso
--            a la API .NET en tiempo real durante ejecuciones largas
-- =====================================================

-- Crear tabla de progreso (ajustar esquema USUARIO según tu configuración)
CREATE TABLE USUARIO.JOB_PROGRESS (
  JOB_ID VARCHAR2(50) PRIMARY KEY,
  PROGRESS_PCT NUMBER(3) DEFAULT 0 CHECK (PROGRESS_PCT BETWEEN 0 AND 100),
  STATUS_MESSAGE VARCHAR2(500),
  LAST_UPDATE TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Índice para consultas por última actualización
CREATE INDEX IDX_JOB_PROGRESS_UPDATE ON USUARIO.JOB_PROGRESS(LAST_UPDATE);

-- Comentarios para documentación
COMMENT ON TABLE USUARIO.JOB_PROGRESS IS 'Tabla para tracking de progreso de jobs de larga duración';
COMMENT ON COLUMN USUARIO.JOB_PROGRESS.JOB_ID IS 'ID único del job (generado por la API)';
COMMENT ON COLUMN USUARIO.JOB_PROGRESS.PROGRESS_PCT IS 'Porcentaje de progreso (0-100)';
COMMENT ON COLUMN USUARIO.JOB_PROGRESS.STATUS_MESSAGE IS 'Mensaje descriptivo del paso actual';
COMMENT ON COLUMN USUARIO.JOB_PROGRESS.LAST_UPDATE IS 'Timestamp de la última actualización';

-- =====================================================
-- Ejemplo de modificación del SP para reportar progreso
-- =====================================================

CREATE OR REPLACE PROCEDURE USUARIO.MOD_TEMPORALES.W_INSERT_NOVDDJJPREV (
  vJOB_ID IN VARCHAR2,  -- ← NUEVO PARÁMETRO
  vTIPOENTRADA IN NUMBER,
  vGRUPOADIC IN NUMBER,
  vTIPOLIQ IN NUMBER,
  vCANTREG IN NUMBER,
  vIDARCHIVO IN NUMBER,
  vPERIODO IN DATE,
  vIDREP IN NUMBER,
  vNROHOJA OUT NUMBER
) AS
  vLINEAS_ERROR VARCHAR2(500);
  vCANT_ERRORES NUMBER := 0;
BEGIN
  -- ========== PASO 1: Validaciones iniciales ==========
  UPDATE USUARIO.JOB_PROGRESS 
  SET PROGRESS_PCT = 10, 
      STATUS_MESSAGE = 'Validando datos de entrada...',
      LAST_UPDATE = SYSTIMESTAMP
  WHERE JOB_ID = vJOB_ID;
  COMMIT; -- IMPORTANTE: hacer commit para que la API vea el cambio
  
  -- Validaciones...
  IF vCANTREG <= 0 THEN
    RAISE_APPLICATION_ERROR(-20001, 'Cantidad de registros inválida');
  END IF;

  -- ========== PASO 2: Verificar registros en tabla temporal ==========
  UPDATE USUARIO.JOB_PROGRESS 
  SET PROGRESS_PCT = 30, 
      STATUS_MESSAGE = 'Verificando registros en tabla temporal...',
      LAST_UPDATE = SYSTIMESTAMP
  WHERE JOB_ID = vJOB_ID;
  COMMIT;
  
  SELECT COUNT(*) INTO vCANT_ERRORES
  FROM USUARIO.TMP_NOV_DDJJ_PREV
  WHERE ESVALIDA = 0;
  
  IF vCANT_ERRORES > 0 THEN
    -- Obtener líneas con error (usar XMLAGG para Oracle 11g)
    SELECT DBMS_LOB.SUBSTR(
             RTRIM(
               XMLAGG(
                 XMLELEMENT(e, TO_CHAR(NRO_LINEA) || ', ')
                 ORDER BY NRO_LINEA
               ).EXTRACT('//text()').getClobVal(),
               ', '
             ),
             500, 1
           )
    INTO vLINEAS_ERROR
    FROM USUARIO.TMP_ARCHIVO
    WHERE ESVALIDA = 0;
    
    RAISE_APPLICATION_ERROR(-20002, 
      'Hay ' || vCANT_ERRORES || ' registros con errores en líneas: ' || vLINEAS_ERROR);
  END IF;

  -- ========== PASO 3: Crear hoja ==========
  UPDATE USUARIO.JOB_PROGRESS 
  SET PROGRESS_PCT = 60, 
      STATUS_MESSAGE = 'Creando registro de hoja...',
      LAST_UPDATE = SYSTIMESTAMP
  WHERE JOB_ID = vJOB_ID;
  COMMIT;
  
  -- Insertar hoja...
  INSERT INTO USUARIO.HOJA (TIPOENTRADA, PERIODO, IDTIPOLIQUIDACION, ...)
  VALUES (vTIPOENTRADA, vPERIODO, vTIPOLIQ, ...)
  RETURNING ID INTO vNROHOJA;

  -- ========== PASO 4: Procesar registros ==========
  UPDATE USUARIO.JOB_PROGRESS 
  SET PROGRESS_PCT = 80, 
      STATUS_MESSAGE = 'Procesando ' || vCANTREG || ' registros...',
      LAST_UPDATE = SYSTIMESTAMP
  WHERE JOB_ID = vJOB_ID;
  COMMIT;
  
  -- INSERT masivo desde tabla temporal...
  INSERT INTO USUARIO.DETALLE_HOJA (...)
  SELECT ... FROM USUARIO.TMP_NOV_DDJJ_PREV;

  -- ========== PASO 5: Finalización ==========
  UPDATE USUARIO.JOB_PROGRESS 
  SET PROGRESS_PCT = 100, 
      STATUS_MESSAGE = 'Hoja creada exitosamente',
      LAST_UPDATE = SYSTIMESTAMP
  WHERE JOB_ID = vJOB_ID;
  COMMIT;
  
EXCEPTION
  WHEN OTHERS THEN
    -- Registrar error en progreso antes de propagar
    UPDATE USUARIO.JOB_PROGRESS 
    SET PROGRESS_PCT = -1, 
        STATUS_MESSAGE = 'Error: ' || SQLERRM,
        LAST_UPDATE = SYSTIMESTAMP
    WHERE JOB_ID = vJOB_ID;
    COMMIT;
    RAISE;
END W_INSERT_NOVDDJJPREV;
/

-- =====================================================
-- Job de limpieza automática (opcional)
-- Ejecutar diariamente para limpiar registros antiguos
-- =====================================================

CREATE OR REPLACE PROCEDURE USUARIO.LIMPIAR_JOB_PROGRESS_ANTIGUOS AS
BEGIN
  DELETE FROM USUARIO.JOB_PROGRESS
  WHERE LAST_UPDATE < SYSTIMESTAMP - INTERVAL '1' DAY;
  
  COMMIT;
  
  DBMS_OUTPUT.PUT_LINE('Registros eliminados: ' || SQL%ROWCOUNT);
END;
/

-- Programar ejecución diaria (ejemplo con DBMS_SCHEDULER)
BEGIN
  DBMS_SCHEDULER.CREATE_JOB (
    job_name        => 'LIMPIAR_JOB_PROGRESS_DIARIO',
    job_type        => 'STORED_PROCEDURE',
    job_action      => 'USUARIO.LIMPIAR_JOB_PROGRESS_ANTIGUOS',
    start_date      => SYSTIMESTAMP,
    repeat_interval => 'FREQ=DAILY; BYHOUR=2; BYMINUTE=0',
    enabled         => TRUE,
    comments        => 'Limpieza diaria de registros de progreso antiguos'
  );
END;
/

-- =====================================================
-- Grants necesarios (ajustar según tu configuración)
-- =====================================================

-- Si el usuario de la API es diferente al dueño de la tabla
GRANT SELECT, INSERT, UPDATE, DELETE ON USUARIO.JOB_PROGRESS TO REPORTES;
