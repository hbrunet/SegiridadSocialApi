using SeguridadSocialApi.Services.DTOs;
using System.Data;

namespace SeguridadSocialApi.Validaciones
{
    /// <summary>
    /// Orquestador que ejecuta todas las reglas de validación registradas
    /// Implementa Chain of Responsibility pattern
    /// </summary>
    public class ValidacionExecutor
    {
        private readonly List<IValidacionRule> _reglas;

        public ValidacionExecutor(IEnumerable<IValidacionRule> reglas)
        {
            // Ordenar reglas por orden de ejecución
            _reglas = reglas.OrderBy(r => r.Orden).ToList();
        }

        /// <summary>
        /// Ejecuta todas las reglas de validación y consolida resultados
        /// </summary>
        public async Task<ValidacionArchivoDto> EjecutarValidacionesAsync(IDbConnection connection, long idArchivo, int totalRegistros)
        {
            var resultado = new ValidacionArchivoDto();

            // Ejecutar todas las reglas
            foreach (var regla in _reglas)
            {
                var detalles = await regla.ValidarAsync(connection, idArchivo);

                if (regla.Tipo == TipoValidacion.Error)
                {
                    resultado.Errores.AddRange(detalles);
                }
                else if (regla.Tipo == TipoValidacion.Advertencia)
                {
                    resultado.Advertencias.AddRange(detalles);
                }
            }

            // Calcular totales
            resultado.RegistrosErrores = resultado.Errores.Count;
            resultado.RegistrosAdvertencias = resultado.Advertencias.Count;
            resultado.RegistrosValidos = totalRegistros - resultado.RegistrosErrores;

            return resultado;
        }

        /// <summary>
        /// Obtiene información de todas las reglas registradas (útil para documentación/debugging)
        /// </summary>
        public List<InfoReglaValidacion> ObtenerReglasRegistradas()
        {
            return _reglas.Select(r => new InfoReglaValidacion
            {
                Nombre = r.NombreRegla,
                Descripcion = r.Descripcion,
                Tipo = r.Tipo.ToString(),
                Orden = r.Orden
            }).ToList();
        }
    }

    /// <summary>
    /// DTO para exponer información sobre las reglas de validación
    /// </summary>
    public class InfoReglaValidacion
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int Orden { get; set; }
    }
}
