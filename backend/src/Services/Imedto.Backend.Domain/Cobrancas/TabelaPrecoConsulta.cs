using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cobrancas;

/// <summary>
/// Preço sugerido de consulta por estabelecimento/profissional (R2).
/// profissional_id == null representa o preço padrão do estabelecimento.
/// </summary>
public class TabelaPrecoConsulta : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    /// <summary>null = preço padrão do estabelecimento.</summary>
    public virtual Guid? ProfissionalId { get; protected set; }
    public virtual decimal ValorSugerido { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected TabelaPrecoConsulta() { }

    public static TabelaPrecoConsulta Criar(
        long estabelecimentoId,
        Guid? profissionalId,
        decimal valorSugerido)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (valorSugerido <= 0)
            throw new BusinessException("Valor sugerido deve ser maior que zero.");

        return new TabelaPrecoConsulta
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalId = profissionalId,
            ValorSugerido = ArredondamentoMonetario.Arredondar(valorSugerido),
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(decimal valorSugerido)
    {
        if (valorSugerido <= 0)
            throw new BusinessException("Valor sugerido deve ser maior que zero.");
        ValorSugerido = ArredondamentoMonetario.Arredondar(valorSugerido);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }
}
