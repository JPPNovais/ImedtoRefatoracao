using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Automacoes.Commands;

public class CriarRegraAutomacaoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string EventoGatilho { get; set; } = string.Empty;
    public string CondicoesJson { get; set; } = "[]";
    public string AcoesJson { get; set; } = "[]";
}

public class AtualizarRegraAutomacaoCommand : ICommand
{
    public long RegraId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string EventoGatilho { get; set; } = string.Empty;
    public string CondicoesJson { get; set; } = "[]";
    public string AcoesJson { get; set; } = "[]";
}

public class AtivarRegraAutomacaoCommand : ICommand
{
    public long RegraId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

public class DesativarRegraAutomacaoCommand : ICommand
{
    public long RegraId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
