using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
    /// <summary>
    /// Valida que no existan CUILs duplicados en el archivo
    /// </summary>
    public class ValidarCuilDuplicadoRule : ValidacionRuleBase
    {
        public override string NombreRegla => "CUIL_DUPLICADO";

        public override string Descripcion =>
            "Verifica que no existan CUILs duplicados dentro del mismo archivo";

        public override TipoValidacion Tipo => TipoValidacion.Error;

        public override int Orden => 10; // Se ejecuta temprano

        protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
         IDbConnection connection,
        long idArchivo)
        {
            var sql = @"
   SELECT 
           CUIL,
            COUNT(*) AS Cantidad,
            MIN(ROWNUM) AS PrimeraLinea
     FROM USUARIO.TMP_NOV_DDJJ_PREV
          WHERE CUIL IS NOT NULL
      GROUP BY CUIL
    HAVING COUNT(*) > 1
   ORDER BY CUIL";

            var duplicados = await connection.QueryAsync<CuilDuplicado>(sql);

            return duplicados.Select(d => CrearDetalle(
                mensaje: $"CUIL {d.Cuil} está duplicado {d.Cantidad} veces en el archivo",
       linea: d.PrimeraLinea,
     columna: 1 // Asumiendo que CUIL está en la columna 1
            )).ToList();
        }

        private class CuilDuplicado
        {
            public string Cuil { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public int PrimeraLinea { get; set; }
        }
    }
}
