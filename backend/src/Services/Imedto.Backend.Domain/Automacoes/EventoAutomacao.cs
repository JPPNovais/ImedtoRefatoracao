using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Automacoes;

/// <summary>
/// Item de fila do worker de automações. É enfileirado por handlers de domain events
/// (ver <c>EnfileirarEventosAutomacao*Handler</c>) com o payload do evento original
/// em JSON, e processado pelo <c>ProcessadorAutomacoesJob</c> via scheduler.
///
/// Backoff exponencial igual ao de <c>JobAgendado</c>: 60s, 120s, 240s, ..., cap 1h,
/// até <see cref="MaxTentativas"/>. Após esgotar, fica em <see cref="StatusEventoAutomacao.Falhou"/>
/// (terminal — investigação manual).
/// </summary>
public class EventoAutomacao : Entity
{
    public const int MaxTentativas = 3;

    public virtual long RegraId { get; protected set; }
    public virtual string PayloadJson { get; protected set; } = "{}";
    public virtual StatusEventoAutomacao Status { get; protected set; }
    public virtual int TentativaN { get; protected set; }
    public virtual DateTime ExecutarEm { get; protected set; }
    public virtual DateTime? ExecutadoEm { get; protected set; }
    public virtual string? UltimaFalha { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected EventoAutomacao() { }

    /// <summary>Enfileira para execução imediata (ou em data futura — uso típico: lembretes).</summary>
    public static EventoAutomacao Enfileirar(long regraId, string payloadJson, DateTime? executarEm = null)
    {
        if (regraId <= 0)
            throw new BusinessException("Regra é obrigatória.");
        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new BusinessException("Payload é obrigatório.");

        return new EventoAutomacao
        {
            RegraId = regraId,
            PayloadJson = payloadJson,
            Status = StatusEventoAutomacao.Pendente,
            TentativaN = 0,
            ExecutarEm = executarEm ?? DateTime.UtcNow,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void MarcarExecutando()
    {
        if (Status != StatusEventoAutomacao.Pendente)
            throw new BusinessException($"Evento de automação não está pendente (status atual: {Status}).");

        Status = StatusEventoAutomacao.Executando;
    }

    public virtual void MarcarConcluido()
    {
        if (Status != StatusEventoAutomacao.Executando)
            throw new BusinessException($"Evento de automação não está executando (status atual: {Status}).");

        Status = StatusEventoAutomacao.Concluido;
        ExecutadoEm = DateTime.UtcNow;
        UltimaFalha = null;
    }

    /// <summary>
    /// Registra falha e aplica backoff. Após <see cref="MaxTentativas"/>, vai para
    /// <see cref="StatusEventoAutomacao.Falhou"/> e não é reagendado automaticamente.
    /// </summary>
    public virtual void MarcarFalhou(string mensagemErro)
    {
        if (Status != StatusEventoAutomacao.Executando)
            throw new BusinessException($"Evento de automação não está executando (status atual: {Status}).");

        TentativaN += 1;
        UltimaFalha = Truncar(mensagemErro, 500);

        if (TentativaN >= MaxTentativas)
        {
            Status = StatusEventoAutomacao.Falhou;
            ExecutadoEm = DateTime.UtcNow;
            return;
        }

        Status = StatusEventoAutomacao.Pendente;
        ExecutarEm = DateTime.UtcNow.Add(CalcularBackoff(TentativaN));
    }

    private static TimeSpan CalcularBackoff(int tentativas)
    {
        // 60s, 120s, 240s — só temos MaxTentativas=3 no V1, então o cap raramente acontece.
        var segundos = 60.0 * Math.Pow(2, tentativas - 1);
        var capado = Math.Min(segundos, 3600.0);
        return TimeSpan.FromSeconds(capado);
    }

    private static string Truncar(string valor, int tamanho)
    {
        if (string.IsNullOrEmpty(valor)) return valor;
        return valor.Length <= tamanho ? valor : valor.Substring(0, tamanho);
    }
}
