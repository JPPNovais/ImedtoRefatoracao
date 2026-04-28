using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Dashboard;

public class DashboardQuery : IQuery<DashboardDto>
{
    public long EstabelecimentoId { get; set; }
}

public class DashboardDto
{
    public int TotalPacientesAtivos { get; set; }
    public int AgendamentosHoje { get; set; }
    public int AgendamentosSemana { get; set; }
    public decimal ReceitasMes { get; set; }
    public decimal DespesasMes { get; set; }
    public decimal SaldoMes { get; set; }
    public int ItensAbaixoMinimo { get; set; }
    public int OrcamentosPendentes { get; set; }
    public int LancamentosVencidos { get; set; }
    public List<ProximoAgendamentoDto> ProximosAgendamentos { get; set; } = new();
    public List<ItemAbaixoMinimoDto> ItensAbaixoMinimoLista { get; set; } = new();
}

public class ProximoAgendamentoDto
{
    public long Id { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public string ProfissionalNome { get; set; } = string.Empty;
    public DateTime InicioPrevisto { get; set; }
    public string TipoServico { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ItemAbaixoMinimoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal QuantidadeAtual { get; set; }
    public decimal QuantidadeMinima { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
}
