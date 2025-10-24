using Microsoft.Extensions.Caching.Memory;
using SeguridadSocialApi.Services.DTOs;
using System.Collections.Concurrent;

namespace SeguridadSocialApi.Services
{
    public interface IJobManager
    {
        string CreateJob(Func<IProgress<int>, CancellationToken, Task<object>> jobAction, string description);
        JobInfoDto? GetJobInfo(string jobId);
        void UpdateProgress(string jobId, int percentage, string? message = null);
        void CompleteJob(string jobId, object result);
        void FailJob(string jobId, string errorMessage);
        bool CancelJob(string jobId);
        void MarkJobAsStarted(string jobId);
        Func<IProgress<int>, CancellationToken, Task<object>>? GetJobAction(string jobId);
        CancellationToken GetCancellationToken(string jobId);
    }

    public class JobManager : IJobManager
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, JobInfoDto> _jobs;
        private readonly ConcurrentDictionary<string, Func<IProgress<int>, CancellationToken, Task<object>>> _jobActions;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens;
        private readonly ILogger<JobManager> _logger;

        public JobManager(IMemoryCache cache, ILogger<JobManager> logger)
        {
            _cache = cache;
            _logger = logger;
            _jobs = new ConcurrentDictionary<string, JobInfoDto>();
            _jobActions = new ConcurrentDictionary<string, Func<IProgress<int>, CancellationToken, Task<object>>>();
            _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        }

        public string CreateJob(Func<IProgress<int>, CancellationToken, Task<object>> jobAction, string description)
        {
            var jobId = Guid.NewGuid().ToString("N");
            var cts = new CancellationTokenSource();
            
            var jobInfo = new JobInfoDto
            {
                JobId = jobId,
                Status = JobStatus.Pending,
                StatusMessage = description,
                ProgressPercentage = 0,
                CreatedAt = DateTime.UtcNow
            };

            _jobs[jobId] = jobInfo;
            _jobActions[jobId] = jobAction;
            _cancellationTokens[jobId] = cts;

            // Cache con expiración de 30 minutos después de completarse
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
            _cache.Set($"job:{jobId}", jobInfo, cacheOptions);

            _logger.LogInformation("Job {JobId} creado: {Description}", jobId, description);
            return jobId;
        }

        public JobInfoDto? GetJobInfo(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                return jobInfo;
            }

            // Intentar recuperar del cache
            return _cache.Get<JobInfoDto>($"job:{jobId}");
        }

        public void UpdateProgress(string jobId, int percentage, string? message = null)
        {
            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                jobInfo.ProgressPercentage = Math.Clamp(percentage, 0, 100);
                if (!string.IsNullOrEmpty(message))
                {
                    jobInfo.StatusMessage = message;
                }
                _cache.Set($"job:{jobId}", jobInfo, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }
        }

        public void CompleteJob(string jobId, object result)
        {
            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                jobInfo.Status = JobStatus.Completed;
                jobInfo.CompletedAt = DateTime.UtcNow;
                jobInfo.Result = result;
                jobInfo.ProgressPercentage = 100;
                
                _cache.Set($"job:{jobId}", jobInfo, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // Mantener resultado 1 hora
                });

                // Limpiar recursos
                _jobActions.TryRemove(jobId, out _);
                _cancellationTokens.TryRemove(jobId, out var cts);
                cts?.Dispose();

                _logger.LogInformation("Job {JobId} completado exitosamente", jobId);
            }
        }

        public void FailJob(string jobId, string errorMessage)
        {
            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                jobInfo.Status = JobStatus.Failed;
                jobInfo.CompletedAt = DateTime.UtcNow;
                jobInfo.ErrorMessage = errorMessage;
                
                _cache.Set($"job:{jobId}", jobInfo, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });

                // Limpiar recursos
                _jobActions.TryRemove(jobId, out _);
                _cancellationTokens.TryRemove(jobId, out var cts);
                cts?.Dispose();

                _logger.LogError("Job {JobId} falló: {Error}", jobId, errorMessage);
            }
        }

        public bool CancelJob(string jobId)
        {
            if (_cancellationTokens.TryGetValue(jobId, out var cts))
            {
                cts.Cancel();
                
                if (_jobs.TryGetValue(jobId, out var jobInfo))
                {
                    jobInfo.Status = JobStatus.Cancelled;
                    jobInfo.CompletedAt = DateTime.UtcNow;
                    jobInfo.StatusMessage = "Job cancelado por el usuario";
                }

                _logger.LogWarning("Job {JobId} cancelado", jobId);
                return true;
            }
            return false;
        }

        public Func<IProgress<int>, CancellationToken, Task<object>>? GetJobAction(string jobId)
        {
            _jobActions.TryGetValue(jobId, out var action);
            return action;
        }

        public CancellationToken GetCancellationToken(string jobId)
        {
            if (_cancellationTokens.TryGetValue(jobId, out var cts))
            {
                return cts.Token;
            }
            return CancellationToken.None;
        }

        public void MarkJobAsStarted(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                jobInfo.Status = JobStatus.Running;
                jobInfo.StartedAt = DateTime.UtcNow;
                _cache.Set($"job:{jobId}", jobInfo);
                _logger.LogInformation("Job {JobId} iniciado", jobId);
            }
        }
    }
}
