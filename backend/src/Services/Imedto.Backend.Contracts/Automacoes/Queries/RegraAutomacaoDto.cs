namespace Imedto.Backend.Contracts.Automacoes.Queries;

public class RegraAutomacaoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string EventoGatilho { get; set; } = string.Empty;
    public string CondicoesJson { get; set; } = "[]";
    public string AcoesJson { get; set; } = "[]";
    public bool Ativa { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class EventoAutomacaoDto
{
    public long Id { get; set; }
    public long RegraId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TentativaN { get; set; }
    public DateTime ExecutarEm { get; set; }
    public DateTime? ExecutadoEm { get; set; }
    public string? UltimaFalha { get; set; }
    public DateTime CriadoEm { get; set; }
}
