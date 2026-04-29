using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Automacoes;

/// <summary>
/// Regra de automação configurada pelo dono do estabelecimento. Liga um evento de
/// domínio (ex: "agendamento-criado") a um conjunto de condições (DSL JSON) e ações
/// (também JSON). Quando o evento é publicado pelo <c>IEventBus</c>, o handler global
/// avalia condições e enfileira <see cref="EventoAutomacao"/>s para execução assíncrona
/// pelo worker (ProcessadorAutomacoesJob).
///
/// O JSON é mantido como string crua no aggregate — interpretação fica em camadas
/// específicas (avaliador/executor) para não acoplar o domínio à serialização.
/// </summary>
public class RegraAutomacao : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string EventoGatilho { get; protected set; } = string.Empty;
    public virtual string CondicoesJson { get; protected set; } = "[]";
    public virtual string AcoesJson { get; protected set; } = "[]";
    public virtual bool Ativa { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected RegraAutomacao() { }

    public static RegraAutomacao Criar(
        long estabelecimentoId,
        string nome,
        string eventoGatilho,
        string condicoesJson,
        string acoesJson)
    {
        Validar(estabelecimentoId, nome, eventoGatilho, condicoesJson, acoesJson);

        return new RegraAutomacao
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            EventoGatilho = eventoGatilho.Trim(),
            CondicoesJson = string.IsNullOrWhiteSpace(condicoesJson) ? "[]" : condicoesJson.Trim(),
            AcoesJson = acoesJson.Trim(),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void AtualizarRegras(
        string nome,
        string eventoGatilho,
        string condicoesJson,
        string acoesJson)
    {
        Validar(EstabelecimentoId, nome, eventoGatilho, condicoesJson, acoesJson);

        Nome = nome.Trim();
        EventoGatilho = eventoGatilho.Trim();
        CondicoesJson = string.IsNullOrWhiteSpace(condicoesJson) ? "[]" : condicoesJson.Trim();
        AcoesJson = acoesJson.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Ativar()
    {
        if (Ativa) return;
        Ativa = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Desativar()
    {
        if (!Ativa) return;
        Ativa = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void Validar(long estabelecimentoId, string nome, string evento, string condicoes, string acoes)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da regra é obrigatório.");
        if (nome.Length > 120)
            throw new BusinessException("Nome da regra não pode exceder 120 caracteres.");
        if (string.IsNullOrWhiteSpace(evento))
            throw new BusinessException("Evento gatilho é obrigatório.");
        if (evento.Length > 60)
            throw new BusinessException("Evento gatilho não pode exceder 60 caracteres.");
        if (string.IsNullOrWhiteSpace(acoes))
            throw new BusinessException("Pelo menos uma ação é obrigatória.");
        // condicoesJson pode ser vazio/[] (regra sem condição = sempre dispara).
    }
}
