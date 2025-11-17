using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
    /// <summary>
    /// Valida rangos válidos para códigos y campos numéricos
    /// </summary>
    public class ValidarRangosCamposRule : ValidacionRuleBase
    {
        public override string NombreRegla => "RANGOS_CAMPOS";

        public override string Descripcion =>
    "Verifica que los campos numéricos estén dentro de rangos válidos";

        public override TipoValidacion Tipo => TipoValidacion.Advertencia;

        public override int Orden => 25;

        protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
              IDbConnection connection,
             long idArchivo)
        {
            var advertencias = new List<DetalleValidacionDto>();

            // Validar CANTHIJOS (cantidad de hijos razonable: 0-20)
            var hijosInvalidos = await connection.QueryAsync<RegistroError>(@"
                SELECT ROWNUM AS Linea, CUIL, CANTHIJOS
  FROM USUARIO.TMP_NOV_DDJJ_PREV
   WHERE CANTHIJOS IS NOT NULL AND (CANTHIJOS < 0 OR CANTHIJOS > 20)");

            advertencias.AddRange(hijosInvalidos.Select(r => CrearDetalle(
             mensaje: $"CUIL {r.Cuil}: CANTHIJOS = {r.CantHijos} parece inusual (rango esperado: 0-20)",
                linea: r.Linea,
           columna: 5
                    )));

            // Validar CANTADHERENTES (cantidad de adherentes razonable: 0-10)
            var adherentesInvalidos = await connection.QueryAsync<RegistroError>(@"
   SELECT ROWNUM AS Linea, CUIL, CANTADHERENTES
       FROM USUARIO.TMP_NOV_DDJJ_PREV
 WHERE CANTADHERENTES IS NOT NULL AND (CANTADHERENTES < 0 OR CANTADHERENTES > 10)");

            advertencias.AddRange(adherentesInvalidos.Select(r => CrearDetalle(
         mensaje: $"CUIL {r.Cuil}: CANTADHERENTES = {r.CantAdherentes} parece inusual (rango esperado: 0-10)",
          linea: r.Linea,
             columna: 13
           )));

            // Validar CANT_DIAS_TRABA (días trabajados en el mes: 0-31)
            var diasInvalidos = await connection.QueryAsync<RegistroError>(@"
   SELECT ROWNUM AS Linea, CUIL, CANT_DIAS_TRABA
           FROM USUARIO.TMP_NOV_DDJJ_PREV
    WHERE CANT_DIAS_TRABA IS NOT NULL AND (CANT_DIAS_TRABA < 0 OR CANT_DIAS_TRABA > 31)");

            advertencias.AddRange(diasInvalidos.Select(r => CrearDetalle(
              mensaje: $"CUIL {r.Cuil}: CANT_DIAS_TRABA = {r.CantDiasTrabajados} es inválido (debe estar entre 0 y 31)",
              linea: r.Linea,
            columna: 40
              )));

            // Validar CANTHORASEXTRA (horas extra razonables: 0-200)
            var horasExtraInvalidas = await connection.QueryAsync<RegistroError>(@"
   SELECT ROWNUM AS Linea, CUIL, CANTHORASEXTRA
        FROM USUARIO.TMP_NOV_DDJJ_PREV
        WHERE CANTHORASEXTRA IS NOT NULL AND (CANTHORASEXTRA < 0 OR CANTHORASEXTRA > 200)");

            advertencias.AddRange(horasExtraInvalidas.Select(r => CrearDetalle(
         mensaje: $"CUIL {r.Cuil}: CANTHORASEXTRA = {r.CantHorasExtra} parece excesivo (rango esperado: 0-200)",
         linea: r.Linea,
           columna: 49
       )));

            // Validar HORASTRAB (horas trabajadas mensuales: 0-400)
            var horasTrabInvalidas = await connection.QueryAsync<RegistroError>(@"
     SELECT ROWNUM AS Linea, CUIL, HORASTRAB
    FROM USUARIO.TMP_NOV_DDJJ_PREV
   WHERE HORASTRAB IS NOT NULL AND (HORASTRAB < 0 OR HORASTRAB > 400)");

            advertencias.AddRange(horasTrabInvalidas.Select(r => CrearDetalle(
             mensaje: $"CUIL {r.Cuil}: HORASTRAB = {r.HorasTrabajadas} parece inválido (rango esperado: 0-400)",
               linea: r.Linea,
             columna: 55
             )));

            return advertencias;
        }

        private class RegistroError
        {
            public int Linea { get; set; }
            public long Cuil { get; set; }
            public int? CantHijos { get; set; }
            public int? CantAdherentes { get; set; }
            public int? CantDiasTrabajados { get; set; }
            public int? CantHorasExtra { get; set; }
            public int? HorasTrabajadas { get; set; }
        }
    }
}
