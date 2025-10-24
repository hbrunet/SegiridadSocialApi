using System.Threading.Tasks;
using Dapper;
using SeguridadSocialApi.Services;

namespace SeguridadSocialApi.Repositories
{
    public class JobProgressRepository : IJobProgressRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public JobProgressRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task InitializeAsync(string jobId, string initialMessage = "Iniciando...")
        {
            const string sql = @"
MERGE INTO SEGSOCIAL.JOB_PROGRESS t
USING (SELECT :JobId AS JOB_ID FROM dual) s
ON (t.JOB_ID = s.JOB_ID)
WHEN MATCHED THEN UPDATE SET t.PROGRESS_PCT = 0, t.STATUS_MESSAGE = :Msg, t.LAST_UPDATE = SYSTIMESTAMP
WHEN NOT MATCHED THEN INSERT (JOB_ID, PROGRESS_PCT, STATUS_MESSAGE, LAST_UPDATE) VALUES (:JobId, 0, :Msg, SYSTIMESTAMP)";

            await _unitOfWork.Connection.ExecuteAsync(sql, new { JobId = jobId, Msg = initialMessage });
        }

        public async Task<(int progressPct, string statusMessage)> GetProgressAsync(string jobId)
        {
            const string sql = @"SELECT NVL(PROGRESS_PCT,0) AS PROGRESS_PCT, NVL(STATUS_MESSAGE,'') AS STATUS_MESSAGE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = :JobId";
            var row = await _unitOfWork.Connection.QueryFirstOrDefaultAsync(sql, new { JobId = jobId });
            if (row == null)
                return (0, "");

            int pct = (int)row.PROGRESS_PCT;
            string msg = (string)row.STATUS_MESSAGE;
            return (pct, msg);
        }

        public async Task UpdateAsync(string jobId, int progressPct, string statusMessage)
        {
            const string sql = @"UPDATE SEGSOCIAL.JOB_PROGRESS SET PROGRESS_PCT = :Pct, STATUS_MESSAGE = :Msg, LAST_UPDATE = SYSTIMESTAMP WHERE JOB_ID = :JobId";
            await _unitOfWork.Connection.ExecuteAsync(sql, new { JobId = jobId, Pct = progressPct, Msg = statusMessage });
        }

        public async Task DeleteAsync(string jobId)
        {
            const string sql = @"DELETE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = :JobId";
            await _unitOfWork.Connection.ExecuteAsync(sql, new { JobId = jobId });
        }

        public async Task<string> ExecuteTestQuickJobAsync(string jobId)
        {
            var parameters = new Oracle.ManagedDataAccess.Client.OracleParameter[]
            {
                new("p_job_id", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2, jobId, System.Data.ParameterDirection.Input),
                new("p_result", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2, 200) { Direction = System.Data.ParameterDirection.Output }
            };

            await _unitOfWork.Connection.ExecuteAsync("SEGSOCIAL.SP_TEST_QUICK_JOB", parameters, commandType: System.Data.CommandType.StoredProcedure);
            
            return parameters[1].Value?.ToString() ?? "No result";
        }

        public async Task<string> ExecuteTestSlowJobAsync(string jobId)
        {
            var parameters = new Oracle.ManagedDataAccess.Client.OracleParameter[]
            {
                new("p_job_id", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2, jobId, System.Data.ParameterDirection.Input),
                new("p_result", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2, 200) { Direction = System.Data.ParameterDirection.Output }
            };

            await _unitOfWork.Connection.ExecuteAsync("SEGSOCIAL.SP_TEST_SLOW_JOB", parameters, commandType: System.Data.CommandType.StoredProcedure);
            
            return parameters[1].Value?.ToString() ?? "No result";
        }
    }
}
