using System.Collections.Concurrent;

namespace SeguridadSocialApi.Services
{
    public class BackgroundJobExecutor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobExecutor> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentQueue<string> _jobQueue;
        private readonly SemaphoreSlim _signal;

        public BackgroundJobExecutor(IServiceProvider serviceProvider, ILogger<BackgroundJobExecutor> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _jobQueue = new ConcurrentQueue<string>();
            _signal = new SemaphoreSlim(0);
        }

        public void EnqueueJob(string jobId)
        {
            _jobQueue.Enqueue(jobId);
            _signal.Release();
            _logger.LogInformation("Job {JobId} encolado para ejecución", jobId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundJobExecutor iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Esperar señal de que hay un job disponible
                    await _signal.WaitAsync(stoppingToken);

                    if (_jobQueue.TryDequeue(out var jobId))
                    {
                        _logger.LogInformation("Procesando job {JobId}", jobId);
                        
                        // Ejecutar en un scope nuevo para obtener servicios scoped
                        _ = Task.Run(async () =>
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var jobManager = scope.ServiceProvider.GetRequiredService<IJobManager>();
                            
                            try
                            {
                                var jobAction = jobManager.GetJobAction(jobId);
                                if (jobAction == null)
                                {
                                    _logger.LogWarning("Job {JobId} no tiene acción asociada", jobId);
                                    return;
                                }

                                jobManager.MarkJobAsStarted(jobId);

                                var progress = new Progress<int>(percentage =>
                                {
                                    jobManager.UpdateProgress(jobId, percentage);
                                });

                                var cancellationToken = jobManager.GetCancellationToken(jobId);

                                // Ejecutar el job con timeout configurable (default 60 minutos, 0 = sin timeout)
                                var timeoutMinutes = _configuration.GetValue<int>("BackgroundJobs:TimeoutMinutes", 60);
                                
                                CancellationToken executionToken;
                                CancellationTokenSource? timeoutCts = null;
                                CancellationTokenSource? linkedCts = null;

                                if (timeoutMinutes > 0)
                                {
                                    // Con timeout configurado
                                    timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
                                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                                        cancellationToken, timeoutCts.Token);
                                    executionToken = linkedCts.Token;
                                    
                                    _logger.LogInformation("Job {JobId} ejecutándose con timeout de {Timeout} minutos", 
                                        jobId, timeoutMinutes);
                                }
                                else
                                {
                                    // Sin timeout (solo cancelación manual)
                                    executionToken = cancellationToken;
                                    _logger.LogWarning("Job {JobId} ejecutándose SIN timeout automático", jobId);
                                }

                                try
                                {
                                    var result = await jobAction(progress, executionToken);
                                    jobManager.CompleteJob(jobId, result);
                                }
                                finally
                                {
                                    timeoutCts?.Dispose();
                                    linkedCts?.Dispose();
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                jobManager.FailJob(jobId, "Job cancelado o timeout excedido");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error ejecutando job {JobId}", jobId);
                                jobManager.FailJob(jobId, $"Error: {ex.Message}");
                            }
                        }, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Shutdown normal
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en BackgroundJobExecutor");
                }
            }

            _logger.LogInformation("BackgroundJobExecutor detenido");
        }
    }
}
