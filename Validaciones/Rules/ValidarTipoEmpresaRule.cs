using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
    /// <summary>
    /// Valida que el tipo de empresa sea uno de los valores permitidos
    /// </summary>
    public class ValidarTipoEmpresaRule : ValidacionRuleBase
    {
        public override string NombreRegla => "TIPO_EMPRESA_VALIDO";

        public override string Descripcion =>
        "Verifica que TIPOEMPRESA tenga uno de los valores permitidos: '3' o 'G'";

        public override TipoValidacion Tipo => TipoValidacion.Error;

        public override int Orden => 16; // Después de CODACTIVIDAD

        protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
          IDbConnection connection,
     long idArchivo)
        {
            var errores = new List<DetalleValidacionDto>();

            // Validar TIPOEMPRESA nulo
            var tipoEmpresaNulo = await connection.QueryAsync<RegistroError>(@"
         SELECT ROWNUM AS Linea, CUIL, TIPOEMPRESA
                FROM USUARIO.TMP_NOV_DDJJ_PREV
      WHERE TIPOEMPRESA IS NULL");

            errores.AddRange(tipoEmpresaNulo.Select(r => CrearDetalle(
       mensaje: $"CUIL {r.Cuil}: TIPOEMPRESA es obligatorio y no puede ser nulo",
  linea: r.Linea,
     columna: 38
    )));

            // Validar TIPOEMPRESA con valor no permitido
            var tipoEmpresaInvalido = await connection.QueryAsync<RegistroError>(@"
      SELECT ROWNUM AS Linea, CUIL, TIPOEMPRESA
     FROM USUARIO.TMP_NOV_DDJJ_PREV
   WHERE TIPOEMPRESA IS NOT NULL
          AND TIPOEMPRESA NOT IN ('3', 'G')");

            errores.AddRange(tipoEmpresaInvalido.Select(r => CrearDetalle(
                   mensaje: $"CUIL {r.Cuil}: TIPOEMPRESA = '{r.TipoEmpresa}' no es válido (valores permitidos: '3', 'G')",
                  linea: r.Linea,
              columna: 38
             )));

            return errores;
        }

        private class RegistroError
        {
            public int Linea { get; set; }
            public long Cuil { get; set; }
            public string? TipoEmpresa { get; set; }
        }
    }
}
