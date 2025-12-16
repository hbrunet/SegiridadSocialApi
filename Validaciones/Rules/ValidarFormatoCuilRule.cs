// <copyright file="ValidarFormatoCuilRule.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using Dapper;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Validaciones.Rules;

/// <summary>
/// Valida que el formato del CUIL sea válido (11 dígitos).
/// </summary>
public class ValidarFormatoCuilRule : ValidacionRuleBase
{
    /// <inheritdoc/>
    public override string NombreRegla => "FORMATO_CUIL";

    /// <inheritdoc/>
    public override string Descripcion =>
"Verifica que el CUIL tenga exactamente 11 dígitos numéricos";

    /// <inheritdoc/>
    public override TipoValidacion Tipo => TipoValidacion.Error;

    /// <inheritdoc/>
    public override int Orden => 5; // Se ejecuta primero

    /// <inheritdoc/>
    protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
  IDbConnection connection,
  long idArchivo)
    {
        var sql = @"
    SELECT 
 ID AS Linea,
       CUIL
      FROM USUARIO.TMP_NOV_DDJJ_PREV
  WHERE CUIL IS NULL
   OR LENGTH(CUIL) != 11
     OR REGEXP_LIKE(CUIL, '[^0-9]')";

        var errores = await connection.QueryAsync<CuilInvalido>(sql);

        return errores.Select(e => CrearDetalle(
          mensaje: $"CUIL inválido: '{e.Cuil ?? "NULL"}' (debe tener 11 dígitos numéricos)",
          linea: e.Linea,
          columna: 1)).ToList();
    }

    private class CuilInvalido
    {
        public int Linea { get; set; }

        public string? Cuil { get; set; }
    }
}
