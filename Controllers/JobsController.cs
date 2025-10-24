using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.DTOs;
using SeguridadSocialApi.Repositories;

namespace SeguridadSocialApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IJobManager _jobManager;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IJobManager jobManager, ILogger<JobsController> logger)
        {
            _jobManager = jobManager;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el estado actual de un job
        /// </summary>
        [HttpGet("{jobId}")]
        public IActionResult GetJobStatus(string jobId)
        {
            var jobInfo = _jobManager.GetJobInfo(jobId);
            if (jobInfo == null)
            {
                return NotFound(new { error = "Job no encontrado o expirado" });
            }

            return Ok(jobInfo);
        }

        /// <summary>
        /// Cancela un job en ejecución
        /// </summary>
        [HttpPost("{jobId}/cancel")]
        public IActionResult CancelJob(string jobId)
        {
            var success = _jobManager.CancelJob(jobId);
            if (!success)
            {
                return NotFound(new { error = "Job no encontrado o ya finalizado" });
            }

            return Ok(new { message = "Job cancelado exitosamente" });
        }

        /// <summary>
        /// Obtiene el resultado de un job completado
        /// </summary>
        [HttpGet("{jobId}/result")]
        public IActionResult GetJobResult(string jobId)
        {
            var jobInfo = _jobManager.GetJobInfo(jobId);
            if (jobInfo == null)
            {
                return NotFound(new { error = "Job no encontrado o expirado" });
            }

            if (jobInfo.Status == JobStatus.Completed)
            {
                return Ok(jobInfo.Result);
            }

            if (jobInfo.Status == JobStatus.Failed)
            {
                return BadRequest(new { error = jobInfo.ErrorMessage });
            }

            return BadRequest(new { error = "Job aún no completado", status = jobInfo.Status });
        }

        /// <summary>
        /// Inicia un job de prueba rápido (30 segundos) para testing
        /// </summary>
        [HttpPost("test/quick")]
        public IActionResult StartTestQuickJob()
        {
            var jobId = _jobManager.CreateJob(
                async (progress, cancellationToken) =>
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var jobProgressRepo = scope.ServiceProvider.GetRequiredService<IJobProgressRepository>();
                    return await jobProgressRepo.ExecuteTestQuickJobAsync(Guid.NewGuid().ToString("N"));
                },
                "Job de prueba rápido (30 segundos)"
            );

            return Accepted(new { job_id = jobId, message = "Job de prueba iniciado", duration = "30 segundos" });
        }

        /// <summary>
        /// Inicia un job de prueba lento (2 minutos) para testing de timeout
        /// </summary>
        [HttpPost("test/slow")]
        public IActionResult StartTestSlowJob()
        {
            var jobId = _jobManager.CreateJob(
                async (progress, cancellationToken) =>
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var jobProgressRepo = scope.ServiceProvider.GetRequiredService<IJobProgressRepository>();
                    return await jobProgressRepo.ExecuteTestSlowJobAsync(Guid.NewGuid().ToString("N"));
                },
                "Job de prueba lento (2 minutos)"
            );

            return Accepted(new { job_id = jobId, message = "Job de prueba iniciado", duration = "2 minutos" });
        }
    }
}
