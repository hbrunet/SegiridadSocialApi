namespace SeguridadSocialApi.Services.DTOs
{
    /// <summary>
    /// Resultado de la validación de un archivo de novedades
    /// </summary>
    public class ValidacionArchivoDto
    {
        /// <summary>
        /// Cantidad de registros válidos en el archivo
        /// </summary>
        public int RegistrosValidos { get; set; }

        /// <summary>
        /// Cantidad de registros con advertencias
        /// </summary>
        public int RegistrosAdvertencias { get; set; }

        /// <summary>
        /// Cantidad de registros con errores
        /// </summary>
        public int RegistrosErrores { get; set; }

        /// <summary>
        /// Lista detallada de errores encontrados
        /// </summary>
        public List<DetalleValidacionDto> Errores { get; set; } = [];

        /// <summary>
        /// Lista detallada de advertencias encontradas
        /// </summary>
        public List<DetalleValidacionDto> Advertencias { get; set; } = [];
    }

    /// <summary>
    /// Detalle de un error o advertencia de validación
    /// </summary>
    public class DetalleValidacionDto
    {
        /// <summary>
        /// Mensaje descriptivo del error o advertencia
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Número de línea donde ocurre el error
        /// </summary>
        public int Linea { get; set; }

        /// <summary>
        /// Número de columna donde ocurre el error
        /// </summary>
        public int Columna { get; set; }
    }
}
