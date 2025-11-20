using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Controllers.Responses
{
    public class UploadResponse
    {
        public string FileName { get; set; } = string.Empty;
        public int CantidadRegistros { get; set; }
        public int ErrorOra { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long IdArchivo { get; set; }
        public decimal SumRem1 { get; set; }
        public decimal SumRem2 { get; set; }
        public decimal SumRem3 { get; set; }

        /// <summary>
        /// Identificador del flujo para mantener la misma sesión de Oracle entre pasos
        /// IMPORTANTE: Usar este FlowId al llamar /validar-archivo o /crear-hoja
        /// </summary>
        public string? FlowId { get; set; }

        /// <summary>
        /// Validaciones del archivo (opcional, se incluye si se ejecutó validación automática)
        /// </summary>
        public ValidacionArchivoDto? Validaciones { get; set; }
    }
}
