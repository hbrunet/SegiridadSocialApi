namespace SeguridadSocialApi.Services.DTOs
{
    public class TipoHojaDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? ProcValidador { get; set; }
        public string? ProcTransformador { get; set; }
    }
}
