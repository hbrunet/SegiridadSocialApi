// <copyright file="ConfiguracionRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Dapper;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.DTOs;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Repositories;

public class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly IUnitOfWork unitOfWork;

    public ConfiguracionRepository(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<List<TipoHojaDto>> GetTiposHojaExternosAsync()
    {
        try
        {
            var sql = @"SELECT TH.ID, TH.NOMBRE, TH.DESCRIPCION, TH.PROCVALIDADOR, TH.PROCTRANSFORMADOR
                            FROM USUARIO.TABTIPOHOJA TH
                            INNER JOIN USUARIO.RELACION_TIPOMEDIO TM ON TH.ID = TM.IDNOV
                            WHERE TM.EXTERNA = 1
                            ORDER BY 1";

            var result = await unitOfWork.Connection.QueryAsync<TipoHojaDto>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener tipos de hoja: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<List<GrupoAdicionalDto>> GetGruposAdicionalesAsync()
    {
        try
        {
            var sql = @"SELECT GA.IDGRUPO, GA.DESCRIPCION
                            FROM USUARIO.GRUPOADICIONAL GA
                            WHERE IDESTADO = 1
                            ORDER BY 1";

            var result = await unitOfWork.Connection.QueryAsync<GrupoAdicionalDto>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener grupos adicionales: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<List<TipoLiquidacionDto>> GetTiposLiquidacionAsync()
    {
        try
        {
            var sql = @"SELECT TL.IDTIPOLIQUIDACION, TL.DESCRIPCION
                            FROM USUARIO.TABTIPOLIQUIDACION TL
                            ORDER BY 1";

            var result = await unitOfWork.Connection.QueryAsync<TipoLiquidacionDto>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener tipos de liquidación: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<List<ReparticionDto>> GetReparticionesSegSocialAsync()
    {
        try
        {
            var sql = "SELECT IDREP, DESCRIPCION FROM SEGSOCIAL.REPARTICION";

            var result = await unitOfWork.Connection.QueryAsync<ReparticionDto>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener reparticiones: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<List<EstadoDto>> GetEstadosHojaAsync()
    {
        try
        {
            var sql = "SELECT * FROM USUARIO.ESTADO";
            var result = await unitOfWork.Connection.QueryAsync<EstadoDto>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener estados: {ex.Message}");
        }
    }
}
