// <copyright file="JobProgressDto.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.DTOs;

/// <summary>
/// DTO para mapear el resultado de consultas a JOB_PROGRESS
/// Usa mapeo explícito para evitar problemas de conversión con Oracle.
/// </summary>
public class JobProgressDto
{
    public int PROGRESS_PCT { get; set; }

    public string STATUS_MESSAGE { get; set; } = string.Empty;
}
