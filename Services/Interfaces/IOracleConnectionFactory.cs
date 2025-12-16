// <copyright file="IOracleConnectionFactory.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;

namespace SeguridadSocialApi.Services.Interfaces;

public interface IOracleConnectionFactory
{
    IDbConnection CreateConnection();
}
