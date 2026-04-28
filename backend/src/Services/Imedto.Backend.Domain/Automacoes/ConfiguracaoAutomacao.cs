using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Automacoes;

public class ConfiguracaoAutomacao : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual bool LembretesHabilitados { get; protected set; }
    public virtual int HorasAntecedenciaLembrete { get; protected set; }
    public virtual bool ExpiracaoOrcamentosHabilitada { get; protected set; }
    public virtual string? EmailRemetente { get; protected set; }
    public virtual DateTime AtualizadoEm { get; protected set; }

    protected ConfiguracaoAutomacao() { }

    public static ConfiguracaoAutomacao PadraoParaEstabelecimento(long estabelecimentoId) => new()
    {
        EstabelecimentoId = estabelecimentoId,
        LembretesHabilitados = false,
        HorasAntecedenciaLembrete = 24,
        ExpiracaoOrcamentosHabilitada = true,
        AtualizadoEm = DateTime.UtcNow
    };

    public void Atualizar(bool lembretesHabilitados, int horasAntecedencia, bool expiracaoHabilitada, string? emailRemetente)
    {
        if (horasAntecedencia < 1 || horasAntecedencia > 72)
            throw new BusinessException("Horas de antecedência deve ser entre 1 e 72.");

        LembretesHabilitados = lembretesHabilitados;
        HorasAntecedenciaLembrete = horasAntecedencia;
        ExpiracaoOrcamentosHabilitada = expiracaoHabilitada;
        EmailRemetente = string.IsNullOrWhiteSpace(emailRemetente) ? null : emailRemetente.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }
}
