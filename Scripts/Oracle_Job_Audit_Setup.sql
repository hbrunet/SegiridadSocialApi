-- =====================================================
-- TABLA DE AUDITORÍA PARA JOBS
-- Registro permanente de todas las ejecuciones
-- =====================================================

-- =====================================================
-- 1. TABLA DE AUDITORÍA
-- =====================================================

CREATE TABLE SEGSOCIAL.JOB_AUDIT (
    AUDIT_ID NUMBER(18) PRIMARY KEY,
    
    -- Identificadores
    JOB_ID VARCHAR2(50) NOT NULL,        -- ID del JobManager (.NET)
    INTERNAL_JOB_ID VARCHAR2(50),       -- ID usado en JOB_PROGRESS (Oracle)
    
    -- Información del Job
    JOB_NAME VARCHAR2(200) NOT NULL,       -- Nombre/descripción del job
    JOB_TYPE VARCHAR2(100),          -- Tipo: FUSION, VALIDACION, CREAR_HOJA, etc.
    
    -- Parámetros de entrada (JSON)
    INPUT_PARAMS CLOB,      -- Parámetros en formato JSON
    
    -- Timestamps
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    STARTED_AT TIMESTAMP,
    COMPLETED_AT TIMESTAMP,
    
    -- Resultado
    STATUS VARCHAR2(20) NOT NULL,      -- COMPLETED, FAILED, CANCELLED, TIMEOUT
    PROGRESS_PCT NUMBER(3) DEFAULT 0,          -- Progreso final (0-100)
    
    -- Resultado detallado (JSON)
    RESULT_DATA CLOB,      -- Resultado en formato JSON
    ERROR_MESSAGE VARCHAR2(4000),    -- Mensaje de error si falló
  
    -- Métricas
    DURATION_SECONDS NUMBER(10,2),       -- Duración en segundos
    REGISTROS_PROCESADOS NUMBER,          -- Cantidad procesada (si aplica)
    REGISTROS_ERRORES NUMBER,         -- Cantidad con errores
    
    -- Contexto
    USUARIO VARCHAR2(100),         -- Usuario que ejecutó
    IP_ADDRESS VARCHAR2(50),         -- IP del cliente
    USER_AGENT VARCHAR2(500),          -- Browser/client info
    
    -- Auditoría
    CREATED_BY VARCHAR2(100) DEFAULT USER,
    MODIFIED_AT TIMESTAMP,
    MODIFIED_BY VARCHAR2(100),
    
    -- Constraints
    CONSTRAINT chk_status CHECK (STATUS IN ('PENDING', 'RUNNING', 'COMPLETED', 'FAILED', 'CANCELLED', 'TIMEOUT')),
    CONSTRAINT chk_progress CHECK (PROGRESS_PCT BETWEEN 0 AND 100)
);

-- Índices para performance
CREATE INDEX idx_job_audit_job_id ON SEGSOCIAL.JOB_AUDIT(JOB_ID);
CREATE INDEX idx_job_audit_internal_id ON SEGSOCIAL.JOB_AUDIT(INTERNAL_JOB_ID);
CREATE INDEX idx_job_audit_created_at ON SEGSOCIAL.JOB_AUDIT(CREATED_AT);
CREATE INDEX idx_job_audit_status ON SEGSOCIAL.JOB_AUDIT(STATUS);
CREATE INDEX idx_job_audit_type ON SEGSOCIAL.JOB_AUDIT(JOB_TYPE);
CREATE INDEX idx_job_audit_usuario ON SEGSOCIAL.JOB_AUDIT(USUARIO);

-- Grants
GRANT SELECT, INSERT, UPDATE ON SEGSOCIAL.JOB_AUDIT TO REPORTES;

-- =====================================================
-- 2. TABLA DE LOGS DETALLADOS (Opcional - para debugging)
-- =====================================================

CREATE TABLE SEGSOCIAL.JOB_AUDIT_LOGS (
    LOG_ID NUMBER(18) PRIMARY KEY,
    AUDIT_ID NUMBER NOT NULL,
    JOB_ID VARCHAR2(50) NOT NULL,
    
    LOG_TIMESTAMP TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
  LOG_LEVEL VARCHAR2(20) NOT NULL,           -- INFO, WARNING, ERROR, DEBUG
    LOG_MESSAGE VARCHAR2(4000),
    PROGRESS_PCT NUMBER(3),
    
    CONSTRAINT fk_job_audit_logs FOREIGN KEY (AUDIT_ID) 
        REFERENCES SEGSOCIAL.JOB_AUDIT(AUDIT_ID) ON DELETE CASCADE,
    CONSTRAINT chk_log_level CHECK (LOG_LEVEL IN ('INFO', 'WARNING', 'ERROR', 'DEBUG'))
);

CREATE INDEX idx_job_audit_logs_audit_id ON SEGSOCIAL.JOB_AUDIT_LOGS(AUDIT_ID);
CREATE INDEX idx_job_audit_logs_job_id ON SEGSOCIAL.JOB_AUDIT_LOGS(JOB_ID);
CREATE INDEX idx_job_audit_logs_timestamp ON SEGSOCIAL.JOB_AUDIT_LOGS(LOG_TIMESTAMP);

GRANT SELECT, INSERT ON SEGSOCIAL.JOB_AUDIT_LOGS TO REPORTES;

-- =====================================================
-- 3. VISTAS DE CONSULTA
-- =====================================================

-- Vista resumen de jobs
CREATE OR REPLACE VIEW SEGSOCIAL.VW_JOB_AUDIT_SUMMARY AS
SELECT 
    JOB_ID,
    INTERNAL_JOB_ID,
    JOB_NAME,
    JOB_TYPE,
    STATUS,
    PROGRESS_PCT,
    CREATED_AT,
    STARTED_AT,
COMPLETED_AT,
    DURATION_SECONDS,
    REGISTROS_PROCESADOS,
  REGISTROS_ERRORES,
    USUARIO,
    CASE 
        WHEN STATUS = 'COMPLETED' THEN '?'
        WHEN STATUS = 'FAILED' THEN '?'
        WHEN STATUS = 'CANCELLED' THEN '??'
        WHEN STATUS = 'TIMEOUT' THEN '??'
        ELSE '??'
    END AS STATUS_ICON,
  CASE 
        WHEN DURATION_SECONDS < 60 THEN ROUND(DURATION_SECONDS, 1) || 's'
        WHEN DURATION_SECONDS < 3600 THEN ROUND(DURATION_SECONDS / 60, 1) || 'm'
        ELSE ROUND(DURATION_SECONDS / 3600, 1) || 'h'
    END AS DURATION_DISPLAY
FROM SEGSOCIAL.JOB_AUDIT;

GRANT SELECT ON SEGSOCIAL.VW_JOB_AUDIT_SUMMARY TO REPORTES;

-- Vista de jobs fallidos recientes
CREATE OR REPLACE VIEW SEGSOCIAL.VW_JOB_FAILURES AS
SELECT 
    JOB_ID,
    JOB_NAME,
    JOB_TYPE,
    ERROR_MESSAGE,
    CREATED_AT,
    DURATION_SECONDS,
    USUARIO
FROM SEGSOCIAL.JOB_AUDIT
WHERE STATUS = 'FAILED'
  AND CREATED_AT > SYSTIMESTAMP - INTERVAL '7' DAY
ORDER BY CREATED_AT DESC;

GRANT SELECT ON SEGSOCIAL.VW_JOB_FAILURES TO REPORTES;

-- Vista de estadísticas por tipo de job
CREATE OR REPLACE VIEW SEGSOCIAL.VW_JOB_STATS AS
SELECT 
    JOB_TYPE,
  COUNT(*) AS TOTAL_JOBS,
    SUM(CASE WHEN STATUS = 'COMPLETED' THEN 1 ELSE 0 END) AS COMPLETED,
    SUM(CASE WHEN STATUS = 'FAILED' THEN 1 ELSE 0 END) AS FAILED,
    SUM(CASE WHEN STATUS = 'CANCELLED' THEN 1 ELSE 0 END) AS CANCELLED,
 SUM(CASE WHEN STATUS = 'TIMEOUT' THEN 1 ELSE 0 END) AS TIMEOUT,
    ROUND(AVG(DURATION_SECONDS), 2) AS AVG_DURATION_SEC,
    ROUND(MAX(DURATION_SECONDS), 2) AS MAX_DURATION_SEC,
    ROUND(MIN(DURATION_SECONDS), 2) AS MIN_DURATION_SEC,
    ROUND(AVG(CASE WHEN STATUS = 'COMPLETED' THEN DURATION_SECONDS END), 2) AS AVG_SUCCESS_DURATION_SEC
FROM SEGSOCIAL.JOB_AUDIT
WHERE CREATED_AT > SYSTIMESTAMP - INTERVAL '30' DAY
GROUP BY JOB_TYPE;

GRANT SELECT ON SEGSOCIAL.VW_JOB_STATS TO REPORTES;

-- =====================================================
-- 4. PROCEDURES HELPER
-- =====================================================

-- Insertar registro de auditoría
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_JOB_AUDIT_INSERT(
    p_job_id IN VARCHAR2,
    p_internal_job_id IN VARCHAR2,
    p_job_name IN VARCHAR2,
    p_job_type IN VARCHAR2,
    p_input_params IN CLOB,
    p_usuario IN VARCHAR2 DEFAULT NULL,
    p_ip_address IN VARCHAR2 DEFAULT NULL,
    p_user_agent IN VARCHAR2 DEFAULT NULL,
    p_audit_id OUT NUMBER
) AS
BEGIN
    INSERT INTO SEGSOCIAL.JOB_AUDIT (
        JOB_ID,
        INTERNAL_JOB_ID,
    JOB_NAME,
        JOB_TYPE,
        INPUT_PARAMS,
     STATUS,
USUARIO,
    IP_ADDRESS,
        USER_AGENT
    ) VALUES (
        p_job_id,
  p_internal_job_id,
    p_job_name,
        p_job_type,
        p_input_params,
    'PENDING',
        NVL(p_usuario, USER),
      p_ip_address,
        p_user_agent
    ) RETURNING AUDIT_ID INTO p_audit_id;
    
    COMMIT;
END;
/

-- Actualizar cuando el job inicia
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_JOB_AUDIT_START(
    p_job_id IN VARCHAR2
) AS
BEGIN
    UPDATE SEGSOCIAL.JOB_AUDIT
    SET STATUS = 'RUNNING',
        STARTED_AT = SYSTIMESTAMP,
        MODIFIED_AT = SYSTIMESTAMP,
        MODIFIED_BY = USER
    WHERE JOB_ID = p_job_id;
    
    COMMIT;
END;
/

-- Actualizar cuando el job completa
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_JOB_AUDIT_COMPLETE(
    p_job_id IN VARCHAR2,
    p_status IN VARCHAR2,
p_progress_pct IN NUMBER,
    p_result_data IN CLOB,
    p_error_message IN VARCHAR2 DEFAULT NULL,
    p_registros_procesados IN NUMBER DEFAULT NULL,
    p_registros_errores IN NUMBER DEFAULT NULL
) AS
    v_started_at TIMESTAMP;
    v_duration NUMBER;
BEGIN
 -- Obtener started_at para calcular duración
    SELECT STARTED_AT INTO v_started_at
    FROM SEGSOCIAL.JOB_AUDIT
    WHERE JOB_ID = p_job_id;
    
    -- Calcular duración
    v_duration := EXTRACT(SECOND FROM (SYSTIMESTAMP - v_started_at)) +
    EXTRACT(MINUTE FROM (SYSTIMESTAMP - v_started_at)) * 60 +
            EXTRACT(HOUR FROM (SYSTIMESTAMP - v_started_at)) * 3600;
    
    -- Actualizar
    UPDATE SEGSOCIAL.JOB_AUDIT
    SET STATUS = p_status,
      COMPLETED_AT = SYSTIMESTAMP,
        PROGRESS_PCT = p_progress_pct,
        RESULT_DATA = p_result_data,
    ERROR_MESSAGE = p_error_message,
        DURATION_SECONDS = v_duration,
        REGISTROS_PROCESADOS = p_registros_procesados,
        REGISTROS_ERRORES = p_registros_errores,
        MODIFIED_AT = SYSTIMESTAMP,
        MODIFIED_BY = USER
    WHERE JOB_ID = p_job_id;
    
    COMMIT;
END;
/

-- Agregar log detallado
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_JOB_AUDIT_LOG(
    p_job_id IN VARCHAR2,
    p_log_level IN VARCHAR2,
    p_log_message IN VARCHAR2,
    p_progress_pct IN NUMBER DEFAULT NULL
) AS
    v_audit_id NUMBER;
BEGIN
    -- Obtener audit_id
    SELECT AUDIT_ID INTO v_audit_id
  FROM SEGSOCIAL.JOB_AUDIT
    WHERE JOB_ID = p_job_id;
    
    INSERT INTO SEGSOCIAL.JOB_AUDIT_LOGS (
        AUDIT_ID,
        JOB_ID,
        LOG_LEVEL,
        LOG_MESSAGE,
        PROGRESS_PCT
    ) VALUES (
      v_audit_id,
 p_job_id,
        p_log_level,
      p_log_message,
     p_progress_pct
    );
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        NULL; -- Job no encontrado, ignorar
END;
/

-- Grants para los procedures
GRANT EXECUTE ON SEGSOCIAL.SP_JOB_AUDIT_INSERT TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_JOB_AUDIT_START TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_JOB_AUDIT_COMPLETE TO REPORTES;
GRANT EXECUTE ON SEGSOCIAL.SP_JOB_AUDIT_LOG TO REPORTES;

-- =====================================================
-- 5. JOB DE LIMPIEZA AUTOMÁTICA (Opcional)
-- =====================================================

-- Eliminar jobs antiguos (más de 90 días)
CREATE OR REPLACE PROCEDURE SEGSOCIAL.SP_JOB_AUDIT_CLEANUP(
    p_days_to_keep IN NUMBER DEFAULT 90
) AS
    v_deleted NUMBER;
BEGIN
    DELETE FROM SEGSOCIAL.JOB_AUDIT
 WHERE CREATED_AT < SYSTIMESTAMP - NUMTODSINTERVAL(p_days_to_keep, 'DAY');
    
    v_deleted := SQL%ROWCOUNT;
    COMMIT;
    
    DBMS_OUTPUT.PUT_LINE('Jobs eliminados: ' || v_deleted);
END;
/

GRANT EXECUTE ON SEGSOCIAL.SP_JOB_AUDIT_CLEANUP TO REPORTES;

-- =====================================================
-- 6. QUERIES DE EJEMPLO
-- =====================================================

-- Ver últimos 20 jobs
SELECT * FROM SEGSOCIAL.VW_JOB_AUDIT_SUMMARY
ORDER BY CREATED_AT DESC
FETCH FIRST 20 ROWS ONLY;

-- Ver jobs fallidos hoy
SELECT 
    JOB_NAME,
    ERROR_MESSAGE,
 DURATION_SECONDS,
    TO_CHAR(CREATED_AT, 'HH24:MI:SS') AS HORA
FROM SEGSOCIAL.JOB_AUDIT
WHERE STATUS = 'FAILED'
  AND TRUNC(CREATED_AT) = TRUNC(SYSTIMESTAMP)
ORDER BY CREATED_AT DESC;

-- Ver estadísticas por tipo de job
SELECT * FROM SEGSOCIAL.VW_JOB_STATS
ORDER BY TOTAL_JOBS DESC;

-- Ver jobs más lentos
SELECT 
    JOB_NAME,
    JOB_TYPE,
    DURATION_SECONDS,
    DURATION_SECONDS / 60 AS DURATION_MINUTES,
    STATUS,
    TO_CHAR(CREATED_AT, 'DD/MM/YYYY HH24:MI') AS FECHA
FROM SEGSOCIAL.JOB_AUDIT
WHERE STATUS = 'COMPLETED'
ORDER BY DURATION_SECONDS DESC
FETCH FIRST 10 ROWS ONLY;

-- Ver jobs de un usuario específico
SELECT 
    JOB_NAME,
    STATUS,
    DURATION_SECONDS,
    TO_CHAR(CREATED_AT, 'DD/MM/YYYY HH24:MI:SS') AS FECHA
FROM SEGSOCIAL.JOB_AUDIT
WHERE USUARIO = 'nombre_usuario'
ORDER BY CREATED_AT DESC;

-- Ver tendencia de errores por día (últimos 30 días)
SELECT 
    TRUNC(CREATED_AT) AS FECHA,
    COUNT(*) AS TOTAL,
    SUM(CASE WHEN STATUS = 'COMPLETED' THEN 1 ELSE 0 END) AS EXITOSOS,
    SUM(CASE WHEN STATUS = 'FAILED' THEN 1 ELSE 0 END) AS FALLIDOS,
    ROUND(SUM(CASE WHEN STATUS = 'FAILED' THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 2) AS PCT_FALLIDOS
FROM SEGSOCIAL.JOB_AUDIT
WHERE CREATED_AT > SYSTIMESTAMP - INTERVAL '30' DAY
GROUP BY TRUNC(CREATED_AT)
ORDER BY FECHA DESC;

-- Ver logs detallados de un job específico
SELECT 
    TO_CHAR(LOG_TIMESTAMP, 'HH24:MI:SS.FF3') AS TIMESTAMP,
    LOG_LEVEL,
    PROGRESS_PCT || '%' AS PROGRESO,
    LOG_MESSAGE
FROM SEGSOCIAL.JOB_AUDIT_LOGS
WHERE JOB_ID = 'tu_job_id_aqui'
ORDER BY LOG_TIMESTAMP;
