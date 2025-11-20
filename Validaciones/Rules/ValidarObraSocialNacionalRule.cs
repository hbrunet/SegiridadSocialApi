using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
    /// <summary>
    /// Valida que para las actividades 46 y 77 exista importe de obra social nacional
    /// </summary>
    public class ValidarObraSocialNacionalRule : ValidacionRuleBase
    {
        public override string NombreRegla => "OBRA_SOCIAL_NACIONAL_REQUERIDA";

        public override string Descripcion =>
            "Verifica que para CODACTIVIDAD 46 y 77 el campo REMUNIMPONIBLE4 (Obra Social Nacional) tenga valor positivo";

        public override TipoValidacion Tipo => TipoValidacion.Error;

        public override int Orden => 21; // Después de REMUNERACION_POSITIVA

        protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
                   IDbConnection connection,
                   long idArchivo)
        {
            var errores = new List<DetalleValidacionDto>();

            // Validar que actividades 46 y 77 tengan REMUNIMPONIBLE4 > 0
            var registrosSinObraSocial = await connection.QueryAsync<RegistroError>(@"
     SELECT ID AS Linea, 
     CUIL, 
CODACTIVIDAD,
      NVL(REMUNIMPONIBLE4, 0) AS RemunImponible4
     FROM USUARIO.TMP_NOV_DDJJ_PREV
    WHERE CODACTIVIDAD IN (46, 77)
            AND (REMUNIMPONIBLE4 IS NULL OR REMUNIMPONIBLE4 <= 0)");

            errores.AddRange(registrosSinObraSocial.Select(r => CrearDetalle(
             mensaje: $"CUIL {r.Cuil}: CODACTIVIDAD {r.CodActividad} requiere REMUNIMPONIBLE4 (Obra Social Nacional) con valor positivo (actual: {r.RemunImponible4})",
                    linea: r.Linea,
             columna: 24
                )));

            return errores;
        }

        private class RegistroError
        {
            public int Linea { get; set; }
            public long Cuil { get; set; }
            public int CodActividad { get; set; }
            public decimal RemunImponible4 { get; set; }
        }
    }
}
