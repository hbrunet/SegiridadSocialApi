namespace SeguridadSocialApi.Services.DTOs
{
    public class HojaDto
    {
        public int Id { get; set; }
        public int NroHoja { get; set; }
        public DateTime? Periodo { get; set; }
        public int IdTipoLiquidacion { get; set; }
        public string? TipoLiquidacion { get; set; }
        public int IdGrupoAdicional { get; set; }
        public int IdEstado { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaAlta { get; set; }
        public int CantidadReg { get; set; }
        public int? IdRep { get; set; }
    }
}