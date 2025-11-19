using Dapper;
using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones.Rules
{
    /// <summary>
    /// Valida que el código de condición sea uno de los valores permitidos
    /// </summary>
    public class ValidarCodigoCondicionRule : ValidacionRuleBase
    {
        public override string NombreRegla => "CODIGO_CONDICION_VALIDO";

        public override string Descripcion =>
         "Verifica que CODCONDICION tenga uno de los valores permitidos: 1, 2, 5, 14";

        public override TipoValidacion Tipo => TipoValidacion.Error;

        public override int Orden => 17; // Después de TIPOEMPRESA

        protected override async Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(
              IDbConnection connection,
                   long idArchivo)
        {
            var errores = new List<DetalleValidacionDto>();

            // Validar CODCONDICION nulo
            var codigoCondicionNulo = await connection.QueryAsync<RegistroError>(@"
      SELECT ID AS Linea, CUIL, CODCONDICION
        FROM USUARIO.TMP_NOV_DDJJ_PREV
           WHERE CODCONDICION IS NULL");

            errores.AddRange(codigoCondicionNulo.Select(r => CrearDetalle(
       mensaje: $"CUIL {r.Cuil}: CODCONDICION es obligatorio y no puede ser nulo",
            linea: r.Linea,
     columna: 7
)));

            // Validar CODCONDICION con valor no permitido
            var codigoCondicionInvalido = await connection.QueryAsync<RegistroError>(@"
             SELECT ID AS Linea, CUIL, CODCONDICION
                FROM USUARIO.TMP_NOV_DDJJ_PREV
                WHERE CODCONDICION IS NOT NULL
        AND CODCONDICION NOT IN (1, 2, 5, 14)");

            errores.AddRange(codigoCondicionInvalido.Select(r => CrearDetalle(
            mensaje: $"CUIL {r.Cuil}: CODCONDICION = {r.CodCondicion} no es válido (valores permitidos: 1, 2, 5, 14)",
            linea: r.Linea,
                columna: 7
            )));

            return errores;
        }

        private class RegistroError
        {
            public int Linea { get; set; }
            public long Cuil { get; set; }
            public int? CodCondicion { get; set; }
        }
    }
}
