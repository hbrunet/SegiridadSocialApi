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
        // Identificador del flujo para mantener la misma sesión de Oracle entre pasos
        public string? FlowId { get; set; }
    }
}
