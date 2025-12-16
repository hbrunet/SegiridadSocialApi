-- ================================================================
-- SCRIPT DE DIAGNÓSTICO: JOB_PROGRESS SIEMPRE DEVUELVE 0
-- ================================================================
-- PROBLEMA RESUELTO: Era un issue de conversión Oracle ? .NET
-- CAUSA: OracleDecimal no se convierte implícitamente a int con dynamic
-- SOLUCIÓN: Usar DTO fuertemente tipado (JobProgressDto)
-- Ver: Docs/FIX_ORACLE_DECIMAL_TO_INT_CONVERSION.md
-- ================================================================

-- 1. Verificar estructura de la tabla
-- ================================================================
SELECT 
  COLUMN_NAME,
    DATA_TYPE,
    DATA_LENGTH,
  DATA_PRECISION,
    DATA_SCALE,
    NULLABLE
FROM 
    ALL_TAB_COLUMNS
WHERE 
  OWNER = 'SEGSOCIAL'
    AND TABLE_NAME = 'JOB_PROGRESS'
ORDER BY 
  COLUMN_ID;

-- ESPERADO:
-- PROGRESS_PCT debe ser NUMBER(3,0) o NUMBER
-- Si es VARCHAR2 ? Problema de tipo de dato

-- 2. Ver todos los registros actuales
-- ================================================================
SELECT 
    JOB_ID,
    PROGRESS_PCT,
    TYPEOF(PROGRESS_PCT) AS TIPO_PROGRESS_PCT,  -- Si existe esta función en tu Oracle
    STATUS_MESSAGE,
    LAST_UPDATE,
    SYSTIMESTAMP - LAST_UPDATE AS EDAD_SEGUNDOS
FROM 
    SEGSOCIAL.JOB_PROGRESS
ORDER BY 
    LAST_UPDATE DESC
FETCH FIRST 20 ROWS ONLY;

-- 3. Verificar si el SP está actualizando
-- ================================================================
-- Ejecutar ANTES de correr un test
SELECT COUNT(*) AS TOTAL_REGISTROS 
FROM SEGSOCIAL.JOB_PROGRESS;

-- Tomar snapshot de los valores actuales
CREATE TABLE SEGSOCIAL.JOB_PROGRESS_SNAPSHOT AS
SELECT * FROM SEGSOCIAL.JOB_PROGRESS;

-- Ejecutar el test desde la API
-- POST /api/novedades/test/fusion-slow-async

-- Después de 30 segundos, verificar cambios
SELECT 
    'ANTES' AS MOMENTO,
    JP_OLD.JOB_ID,
    JP_OLD.PROGRESS_PCT AS PROGRESS_ANTES,
    JP_OLD.STATUS_MESSAGE AS MESSAGE_ANTES,
    JP_OLD.LAST_UPDATE AS UPDATE_ANTES
FROM 
    SEGSOCIAL.JOB_PROGRESS_SNAPSHOT JP_OLD
UNION ALL
SELECT 
    'AHORA' AS MOMENTO,
    JP.JOB_ID,
    JP.PROGRESS_PCT AS PROGRESS_AHORA,
    JP.STATUS_MESSAGE AS MESSAGE_AHORA,
    JP.LAST_UPDATE AS UPDATE_AHORA
FROM 
    SEGSOCIAL.JOB_PROGRESS JP
WHERE 
    JP.JOB_ID IN (SELECT JOB_ID FROM SEGSOCIAL.JOB_PROGRESS_SNAPSHOT)
ORDER BY 
    JOB_ID, MOMENTO;

-- Limpiar tabla temporal
DROP TABLE SEGSOCIAL.JOB_PROGRESS_SNAPSHOT;

-- 4. Verificar si hay COMMITS pendientes
-- ================================================================
-- Ver transacciones activas
SELECT 
    s.sid,
    s.serial#,
    s.username,
    s.status,
    s.sql_id,
  t.start_time,
    t.used_ublk AS UNDO_BLOCKS
FROM 
    v$session s
    LEFT JOIN v$transaction t ON s.saddr = t.ses_addr
WHERE 
    s.username = 'SEGSOCIAL'
    AND s.status = 'ACTIVE';

-- 5. Test manual de actualización
-- ================================================================
-- Crear un registro de prueba
MERGE INTO SEGSOCIAL.JOB_PROGRESS t
USING (SELECT 'TEST_MANUAL_123' AS JOB_ID FROM dual) s
ON (t.JOB_ID = s.JOB_ID)
WHEN MATCHED THEN 
    UPDATE SET t.PROGRESS_PCT = 0, t.STATUS_MESSAGE = 'Iniciando test manual', t.LAST_UPDATE = SYSTIMESTAMP
WHEN NOT MATCHED THEN 
    INSERT (JOB_ID, PROGRESS_PCT, STATUS_MESSAGE, LAST_UPDATE) 
    VALUES ('TEST_MANUAL_123', 0, 'Iniciando test manual', SYSTIMESTAMP);
COMMIT;

-- Verificar que se creó
SELECT * FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = 'TEST_MANUAL_123';

-- Actualizar manualmente
UPDATE SEGSOCIAL.JOB_PROGRESS 
SET PROGRESS_PCT = 50, STATUS_MESSAGE = 'Progreso al 50%', LAST_UPDATE = SYSTIMESTAMP 
WHERE JOB_ID = 'TEST_MANUAL_123';
COMMIT;

-- Verificar que se actualizó
SELECT * FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = 'TEST_MANUAL_123';

-- Si esto funciona, el problema está en el SP

-- Limpiar
DELETE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = 'TEST_MANUAL_123';
COMMIT;

-- 6. Verificar el código del SP
-- ================================================================
SELECT 
 LINE,
    TEXT
FROM 
    ALL_SOURCE
WHERE 
    OWNER = 'SEGSOCIAL'
  AND NAME = 'SP_TEST_FUSION_SLOW'
    AND TYPE = 'PROCEDURE'
ORDER BY 
LINE;

-- Buscar llamadas a UPDATE_JOB_PROGRESS
SELECT 
    LINE,
    TEXT
FROM 
    ALL_SOURCE
WHERE 
    OWNER = 'SEGSOCIAL'
    AND UPPER(TEXT) LIKE '%UPDATE_JOB_PROGRESS%'
    AND NAME IN ('SP_TEST_FUSION_SLOW', 'SP_TEST_FUSION_QUICK', 'JOBS_MONITOR')
ORDER BY 
    NAME, LINE;

-- 7. Verificar permisos
-- ================================================================
SELECT 
PRIVILEGE,
    GRANTABLE
FROM 
    DBA_TAB_PRIVS
WHERE 
    GRANTEE = 'SEGSOCIAL'
    AND TABLE_NAME = 'JOB_PROGRESS'
    AND OWNER = 'SEGSOCIAL';

-- ================================================================
-- RESULTADOS ESPERADOS
-- ================================================================

-- Si PROGRESS_PCT es VARCHAR2 en lugar de NUMBER:
-- ? El problema es el tipo de dato

-- Si el UPDATE manual funciona pero el SP no actualiza:
-- ? El SP no está llamando a UPDATE_JOB_PROGRESS o falta COMMIT

-- Si hay transacciones activas sin commit:
-- ? El SP no hace COMMIT después de actualizar

-- Si PROGRESS_PCT siempre es 0 incluso después del UPDATE manual:
-- ? Problema de sesión/conexión (pooling)
