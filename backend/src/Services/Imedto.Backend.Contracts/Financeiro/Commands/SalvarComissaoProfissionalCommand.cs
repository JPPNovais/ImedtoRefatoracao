using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class SalvarComissaoProfissionalCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    /// <summary>Percentual de comissão para consultas (null = não altera).</summary>
    public decimal? PercentualConsulta { get; set; }
    /// <summary>Percentual de comissão para procedimentos (null = não altera).</summary>
    public decimal? PercentualProcedimento { get; set; }
    /// <summary>Quem executa — deve ser Dono (R16/CA177).</summary>
    public bool EhDono { get; set; }
}
