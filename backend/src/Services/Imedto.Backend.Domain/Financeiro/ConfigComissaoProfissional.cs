using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Configuração de percentual de comissão por (estabelecimento, profissional, tipo).
/// UNIQUE (estabelecimento_id, profissional_usuario_id, tipo) — 1 config por combinação.
/// Quando ausente, o sistema aplica o default de 30% (ComissaoConfig.PercentualPadrao).
/// </summary>
public class ConfigComissaoProfissional : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual TipoComissao Tipo { get; protected set; }
    public virtual decimal Percentual { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ConfigComissaoProfissional() { }

    public static ConfigComissaoProfissional Criar(
        long estabelecimentoId,
        Guid profissionalUsuarioId,
        TipoComissao tipo,
        decimal percentual)
    {
        Validar(estabelecimentoId, profissionalUsuarioId, percentual);
        return new ConfigComissaoProfissional
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            Tipo = tipo,
            Percentual = percentual,
            CriadoEm = DateTime.UtcNow
        };
    }

    public void Atualizar(decimal percentual)
    {
        if (percentual < 0 || percentual > 100)
            throw new BusinessException("Percentual deve estar entre 0 e 100.");
        Percentual = percentual;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void Validar(long estabelecimentoId, Guid profissionalUsuarioId, decimal percentual)
    {
        if (estabelecimentoId <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (profissionalUsuarioId == Guid.Empty) throw new BusinessException("Profissional é obrigatório.");
        if (percentual < 0 || percentual > 100)
            throw new BusinessException("Percentual deve estar entre 0 e 100.");
    }
}
