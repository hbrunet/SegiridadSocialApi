using FluentValidation;
using SeguridadSocialApi.Controllers.Requests;

namespace SeguridadSocialApi.Validators
{
    public class CrearHojaRequestValidator : AbstractValidator<CrearHojaRequest>
    {
        public CrearHojaRequestValidator()
        {
            RuleFor(x => x.IdArchivo)
                .GreaterThan(0)
                .WithMessage("El IdArchivo debe ser mayor a 0");

            RuleFor(x => x.TipoNovedad)
                .GreaterThan(0)
                .WithMessage("El TipoNovedad debe ser mayor a 0");

            RuleFor(x => x.GrupoAdicional)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El GrupoAdicional debe ser mayor o igual a 0");

            RuleFor(x => x.TipoLiquidacion)
                .GreaterThan(0)
                .WithMessage("El TipoLiquidacion debe ser mayor a 0");

            RuleFor(x => x.CantidadRegistros)
                .GreaterThan(0)
                .WithMessage("La CantidadRegistros debe ser mayor a 0");

            RuleFor(x => x.Periodo)
                .NotEmpty()
                .WithMessage("El Periodo es requerido")
                .Must(BeValidDate)
                .WithMessage("El Periodo debe ser una fecha válida");

            RuleFor(x => x.IdRep)
                .GreaterThan(0)
                .WithMessage("El IdRep debe ser mayor a 0");
        }

        private bool BeValidDate(DateTime date)
        {
            return date != default(DateTime) && date > DateTime.MinValue;
        }
    }
}
