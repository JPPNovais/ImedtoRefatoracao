using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Relatorios;

public class RelatorioAgendamentosQuery : IQuery<RelatorioAgendamentosDto>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
}

public class RelatorioAgendamentosDto
{
    public int Total { get; set; }
    public List<AgendamentosPorStatusDto> PorStatus { get; set; } = new();
    public List<AgendamentosPorDiaDto> PorDia { get; set; } = new();
}

public class AgendamentosPorStatusDto
{
    public string Status { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class AgendamentosPorDiaDto
{
    public DateOnly Data { get; set; }
    public int Quantidade { get; set; }
}
