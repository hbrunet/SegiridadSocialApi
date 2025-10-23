namespace SeguridadSocialApi.Services.DTOs
{
    public class HojasPaginadasDto
    {
        public int TotalRegistros { get; set; }
        public List<HojaDto> Hojas { get; set; } = new List<HojaDto>();
    }
}
