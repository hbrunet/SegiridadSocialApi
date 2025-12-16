// <copyright file="ValidarConsistenciaCamposRule.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using Dapper;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Validaciones.Rules;

/// <summary>
/// Valida consistencia entre campos relacionados.
/// </summary>
public class ValidarConsistenciaCamposRule : ValidacionRuleBase
{
    /// <inheritdoc/>
    public override string NombreRegla => "CONSISTENCIA_CAMPOS";

    /// <inheritdoc/>
    public override string Descripcion =>
"Verifica consistencia lógica entre campos relacionados (ej: cónyuge/hijos, remuneraciones)";

    /// <inheritdoc/>
    public override TipoValidacion Tipo => TipoValidacion.Advertencia;

    /// <inheritdoc/>
    public override int Orden => 30;

    /// <inheritdoc/>
    protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
IDbConnection connection,
long idArchivo)
    {
        var advertencias = new List<DetalleValidacionDto>();

        // Validar: Si tiene cónyuge pero 0 hijos (puede ser válido pero inusual)
        var conyugeSinHijos = await connection.QueryAsync<RegistroError>(@"
    SELECT ID AS Linea, CUIL, CONYUGE, CANTHIJOS
      FROM USUARIO.TMP_NOV_DDJJ_PREV
    WHERE CONYUGE = 'S' 
        AND (CANTHIJOS IS NULL OR CANTHIJOS = 0)");

        advertencias.AddRange(conyugeSinHijos.Select(r => CrearDetalle(
       mensaje: $"CUIL {r.Cuil}: Tiene cónyuge pero 0 hijos (verificar si es correcto)",
       linea: r.Linea,
       columna: 4)));

        // Validar: REMUNTOTAL debería ser >= suma de remuneraciones imponibles
        //           var remuneracionesInconsistentes = await connection.QueryAsync<RegistroError>(@"
        //     SELECT ID AS Linea, CUIL, REMUNTOTAL,
        // (NVL(REMUNIMPONIBLE1,0) + NVL(REMUNIMPONIBLE2,0) + NVL(REMUNIMPONIBLE3,0)) AS SumaImponibles
        //  FROM USUARIO.TMP_NOV_DDJJ_PREV
        // WHERE REMUNTOTAL IS NOT NULL
        // AND REMUNTOTAL < (NVL(REMUNIMPONIBLE1,0) + NVL(REMUNIMPONIBLE2,0) + NVL(REMUNIMPONIBLE3,0))");

        // advertencias.AddRange(remuneracionesInconsistentes.Select(r => CrearDetalle(
        //         mensaje: $"CUIL {r.Cuil}: REMUNTOTAL ({r.RemunTotal}) es menor que suma de imponibles ({r.SumaImponibles})",
        //             linea: r.Linea,
        //           columna: 14
        //         )));

        // Validar: Si tiene ASIGFAMPAGADAS > 0 debe tener CANTHIJOS > 0
        var asigFamSinHijos = await connection.QueryAsync<RegistroError>(@"
        SELECT ID AS Linea, CUIL, ASIGFAMPAGADAS, CANTHIJOS
 FROM USUARIO.TMP_NOV_DDJJ_PREV
   WHERE ASIGFAMPAGADAS > 0 
   AND (CANTHIJOS IS NULL OR CANTHIJOS = 0)");

        advertencias.AddRange(asigFamSinHijos.Select(r => CrearDetalle(
          mensaje: $"CUIL {r.Cuil}: Tiene asignaciones familiares pagadas pero 0 hijos declarados",
          linea: r.Linea,
          columna: 16)));

        // Validar: CANT_DIAS_TRABA = 0 pero tiene remuneraciones
        var sinDiasConRemun = await connection.QueryAsync<RegistroError>(@"
     SELECT ID AS Linea, CUIL, CANT_DIAS_TRABA
         FROM USUARIO.TMP_NOV_DDJJ_PREV
   WHERE (CANT_DIAS_TRABA IS NULL OR CANT_DIAS_TRABA = 0)
    AND (NVL(REMUNIMPONIBLE1,0) + NVL(REMUNIMPONIBLE2,0) + NVL(REMUNIMPONIBLE3,0)) > 0");

        advertencias.AddRange(sinDiasConRemun.Select(r => CrearDetalle(
     mensaje: $"CUIL {r.Cuil}: Tiene remuneraciones pero 0 días trabajados declarados",
     linea: r.Linea,
     columna: 40)));

        // Validar: HORAS_EXTRA > 0 pero CANTHORASEXTRA = 0
        var horasExtraInconsistentes = await connection.QueryAsync<RegistroError>(@"
   SELECT ID AS Linea, CUIL, HORAS_EXTRA, CANTHORASEXTRA
    FROM USUARIO.TMP_NOV_DDJJ_PREV
   WHERE HORAS_EXTRA > 0 
  AND (CANTHORASEXTRA IS NULL OR CANTHORASEXTRA = 0)");

        advertencias.AddRange(horasExtraInconsistentes.Select(r => CrearDetalle(
  mensaje: $"CUIL {r.Cuil}: Tiene monto de horas extra pero cantidad de horas = 0",
  linea: r.Linea,
  columna: 33)));

        return advertencias;
    }

    private class RegistroError
    {
        public int Linea { get; set; }

        public long Cuil { get; set; }

        public string? Conyuge { get; set; }

        public int? CantHijos { get; set; }

        public decimal? RemunTotal { get; set; }

        public decimal? SumaImponibles { get; set; }

        public decimal? AsigFamPagadas { get; set; }

        public int? CantDiasTrabajados { get; set; }

        public decimal? HorasExtra { get; set; }

        public int? CantHorasExtra { get; set; }
    }
}
