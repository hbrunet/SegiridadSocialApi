// <copyright file="ValidarCamposObligatoriosGttRule.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using Dapper;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Validaciones.Rules;

/// <summary>
/// Valida que los campos obligatorios de la GTT no estén vacíos o nulos.
/// </summary>
public class ValidarCamposObligatoriosGttRule : ValidacionRuleBase
{
    /// <inheritdoc/>
    public override string NombreRegla => "CAMPOS_OBLIGATORIOS_GTT";

    /// <inheritdoc/>
    public override string Descripcion =>
"Verifica que los campos obligatorios (CUIL, APENOM, Remuneraciones) tengan valores válidos";

    /// <inheritdoc/>
    public override TipoValidacion Tipo => TipoValidacion.Error;

    /// <inheritdoc/>
    public override int Orden => 8;

    /// <inheritdoc/>
    protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
   IDbConnection connection,
   long idArchivo)
    {
        var errores = new List<DetalleValidacionDto>();

        // Validar CUIL vacío o nulo
        var cuilVacio = await connection.QueryAsync<RegistroError>(@"
      SELECT ID AS Linea, CUIL
       FROM USUARIO.TMP_NOV_DDJJ_PREV
         WHERE CUIL IS NULL");

        errores.AddRange(cuilVacio.Select(r => CrearDetalle(
 mensaje: "CUIL es obligatorio y no puede ser nulo",
 linea: r.Linea,
 columna: 2)));

        // Validar APENOM (Apellido y Nombre) vacío
        var apenomVacio = await connection.QueryAsync<RegistroError>(@"
 SELECT ID AS Linea, CUIL
FROM USUARIO.TMP_NOV_DDJJ_PREV
 WHERE APENOM IS NULL OR TRIM(APENOM) = ''");

        errores.AddRange(apenomVacio.Select(r => CrearDetalle(
       mensaje: $"CUIL {r.Cuil?.ToString() ?? "N/A"}: APENOM (Apellido y Nombre) es obligatorio",
       linea: r.Linea,
       columna: 3)));

        // Validar que al menos una remuneración imponible tenga valor > 0
        var todasRemuneracionesVacias = await connection.QueryAsync<RegistroError>(@"
 SELECT ID AS Linea, CUIL
   FROM USUARIO.TMP_NOV_DDJJ_PREV
  WHERE CODSITUACION <> 13
    AND (REMUNIMPONIBLE1 IS NULL OR REMUNIMPONIBLE1 = 0)
    AND (REMUNIMPONIBLE2 IS NULL OR REMUNIMPONIBLE2 = 0)
  AND (REMUNIMPONIBLE3 IS NULL OR REMUNIMPONIBLE3 = 0)
   AND (REMUNIMPONIBLE4 IS NULL OR REMUNIMPONIBLE4 = 0)
   AND (REMUNIMPONIBLE5 IS NULL OR REMUNIMPONIBLE5 = 0)
    AND (REMUNIMPONIBLE6 IS NULL OR REMUNIMPONIBLE6 = 0)
  AND (REMUNIMPONIBLE7 IS NULL OR REMUNIMPONIBLE7 = 0)
      AND (REMUNIMPONIBLE8 IS NULL OR REMUNIMPONIBLE8 = 0)
       AND (REMUNIMPONIBLE9 IS NULL OR REMUNIMPONIBLE9 = 0)
     AND (REMUNIMPONIBLE11 IS NULL OR REMUNIMPONIBLE11 = 0)");

        errores.AddRange(todasRemuneracionesVacias.Select(r => CrearDetalle(
mensaje: $"CUIL {r.Cuil}: Debe tener al menos una remuneración imponible con valor > 0",
linea: r.Linea,
columna: 15)));

        // Validar campos de códigos obligatorios
        var codigosSituacionInvalidos = await connection.QueryAsync<RegistroError>(@"
    SELECT ID AS Linea, CUIL
           FROM USUARIO.TMP_NOV_DDJJ_PREV
        WHERE CODSITUACION IS NULL");

        errores.AddRange(codigosSituacionInvalidos.Select(r => CrearDetalle(
       mensaje: $"CUIL {r.Cuil}: CODSITUACION es obligatorio",
       linea: r.Linea,
       columna: 6)));

        return errores;
    }

    private class RegistroError
    {
        public int Linea { get; set; }

        public long? Cuil { get; set; }
    }
}
