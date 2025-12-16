// <copyright file="IUnitOfWork.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;

namespace SeguridadSocialApi.Services.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }

    IDbTransaction? Transaction { get; }

    void BeginTransaction();

    void Commit();

    void Rollback();

    void UseExternalConnection(IDbConnection externalConnection, bool ownsConnection = false);
}
