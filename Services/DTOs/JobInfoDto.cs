// <copyright file="JobInfoDto.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.DTOs;

public enum JobStatus
{
    Pending = 0,    // En cola esperando ejecución
    Running = 1,    // Ejecutándose actualmente
    Completed = 2,  // Completado exitosamente
    Failed = 3,     // Falló con error
    Cancelled = 4,   // Cancelado manualmente
}

public class JobInfoDto
{
    public required string JobId { get; set; }

    public JobStatus Status { get; set; }

    public string? StatusMessage { get; set; }

    public int ProgressPercentage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public object? Result { get; set; }

    public string? ErrorMessage { get; set; }

    public TimeSpan? ElapsedTime => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;
}

public class StartJobResponse
{
    public required string JobId { get; set; }

    public string Message { get; set; } = "Job iniciado. Use el jobId para consultar el estado.";
}
