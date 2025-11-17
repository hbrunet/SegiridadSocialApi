using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
    /// <summary>
    /// Valida que las remuneraciones sean valores positivos
    /// </summary>
    public class ValidarRemuneracionPositivaRule : ValidacionRuleBase
    {
        public override string NombreRegla => "REMUNERACION_POSITIVA";

        public override string Descripcion =>
             "Verifica que todas las remuneraciones sean valores mayores o iguales a cero";

        public override TipoValidacion Tipo => TipoValidacion.Error;

        public override int Orden => 20;

        protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
   IDbConnection connection,
   long idArchivo)
        {
            var sql = @"
    SELECT 
       ROWNUM AS Linea,
      CUIL,
             CASE 
     WHEN REMUNIMPONIBLE1 < 0 THEN 'REMUNIMPONIBLE1'
           WHEN REMUNIMPONIBLE2 < 0 THEN 'REMUNIMPONIBLE2'
  WHEN REMUNIMPONIBLE3 < 0 THEN 'REMUNIMPONIBLE3'
      END AS Columna,
        CASE 
        WHEN REMUNIMPONIBLE1 < 0 THEN REMUNIMPONIBLE1
    WHEN REMUNIMPONIBLE2 < 0 THEN REMUNIMPONIBLE2
      WHEN REMUNIMPONIBLE3 < 0 THEN REMUNIMPONIBLE3
          END AS ValorNegativo
    FROM USUARIO.TMP_NOV_DDJJ_PREV
     WHERE REMUNIMPONIBLE1 < 0 
  OR REMUNIMPONIBLE2 < 0 
          OR REMUNIMPONIBLE3 < 0";

            var errores = await connection.QueryAsync<RemuneracionNegativa>(sql);

            return errores.Select(e => CrearDetalle(
    mensaje: $"CUIL {e.Cuil}: {e.Columna} tiene valor negativo ({e.ValorNegativo})",
     linea: e.Linea,
 columna: ObtenerNumeroColumna(e.Columna)
     )).ToList();
        }

        private int ObtenerNumeroColumna(string nombreColumna)
        {
            return nombreColumna switch
            {
                "REMUNIMPONIBLE1" => 5,
                "REMUNIMPONIBLE2" => 6,
                "REMUNIMPONIBLE3" => 7,
                _ => 0
            };
        }

        private class RemuneracionNegativa
        {
            public int Linea { get; set; }
            public string Cuil { get; set; } = string.Empty;
            public string Columna { get; set; } = string.Empty;
            public decimal ValorNegativo { get; set; }
        }
    }
}
