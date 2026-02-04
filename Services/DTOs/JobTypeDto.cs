// <copyright file="JobTypeDto.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.DTOs;

public class JobTypeDto
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Endpoint { get; set; }

    public string? Method { get; set; }

    public bool Enabled { get; set; }
}
