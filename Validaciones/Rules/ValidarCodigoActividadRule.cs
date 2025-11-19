using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
    /// <summary>
    /// Valida que el código de actividad sea uno de los valores permitidos
    /// </summary>
    public class ValidarCodigoActividadRule : ValidacionRuleBase
    {
        public override string NombreRegla => "CODIGO_ACTIVIDAD_VALIDO";

        public override string Descripcion =>
      "Verifica que CODACTIVIDAD tenga uno de los valores permitidos: 19, 32, 45, 52, 65, 76, 77, 85, 913";

        public override TipoValidacion Tipo => TipoValidacion.Error;

        public override int Orden => 15; // Después de campos obligatorios, antes de duplicados

        protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
            IDbConnection connection,
long idArchivo)
        {
            var errores = new List<DetalleValidacionDto>();

            // Validar CODACTIVIDAD nulo
            var codigoActividadNulo = await connection.QueryAsync<RegistroError>(@"
    SELECT ID AS Linea, CUIL, CODACTIVIDAD
    FROM USUARIO.TMP_NOV_DDJJ_PREV
WHERE CODACTIVIDAD IS NULL");

            errores.AddRange(codigoActividadNulo.Select(r => CrearDetalle(
          mensaje: $"CUIL {r.Cuil}: CODACTIVIDAD es obligatorio y no puede ser nulo",
         linea: r.Linea,
    columna: 8
         )));

            // Validar CODACTIVIDAD con valor no permitido
            var codigoActividadInvalido = await connection.QueryAsync<RegistroError>(@"
   SELECT ID AS Linea, CUIL, CODACTIVIDAD
       FROM USUARIO.TMP_NOV_DDJJ_PREV
       WHERE CODACTIVIDAD IS NOT NULL
          AND CODACTIVIDAD NOT IN (19, 32, 46, 52, 65, 76, 77, 85, 913)");

            errores.AddRange(codigoActividadInvalido.Select(r => CrearDetalle(
         mensaje: $"CUIL {r.Cuil}: CODACTIVIDAD = {r.CodActividad} no es válido (valores permitidos: 19, 32, 46, 52, 65, 76, 77, 85, 913)",
                  linea: r.Linea,
                      columna: 8
         )));

            return errores;
        }

        private class RegistroError
        {
            public int Linea { get; set; }
            public long Cuil { get; set; }
            public int? CodActividad { get; set; }
        }
    }
}
