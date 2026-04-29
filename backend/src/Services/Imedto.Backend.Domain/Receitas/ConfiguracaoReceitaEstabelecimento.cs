using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Configuração de receita por estabelecimento (papel timbrado, cabeçalho/rodapé,
/// emissor padrão). PK = <see cref="EstabelecimentoId"/> — relação 1:1.
///
/// Não herda de <see cref="Entity"/> porque a chave é o <c>EstabelecimentoId</c>
/// (não há identidade autoincrementada própria).
/// </summary>
public class ConfiguracaoReceitaEstabelecimento
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string? CabecalhoHtml { get; protected set; }
    public virtual string? RodapeHtml { get; protected set; }
    public virtual long? ModeloPadraoId { get; protected set; }
    public virtual string? EmissorPadrao { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected ConfiguracaoReceitaEstabelecimento() { }

    public static ConfiguracaoReceitaEstabelecimento CriarPadrao(long estabelecimentoId)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");

        return new ConfiguracaoReceitaEstabelecimento
        {
            EstabelecimentoId = estabelecimentoId
        };
    }

    public virtual void Atualizar(
        string? cabecalhoHtml,
        string? rodapeHtml,
        long? modeloPadraoId,
        string? emissorPadrao)
    {
        if (emissorPadrao is not null && emissorPadrao.Length > 80)
            throw new BusinessException("Emissor padrão excede 80 caracteres.");

        CabecalhoHtml = string.IsNullOrWhiteSpace(cabecalhoHtml) ? null : cabecalhoHtml;
        RodapeHtml = string.IsNullOrWhiteSpace(rodapeHtml) ? null : rodapeHtml;
        ModeloPadraoId = modeloPadraoId is > 0 ? modeloPadraoId : null;
        EmissorPadrao = string.IsNullOrWhiteSpace(emissorPadrao) ? null : emissorPadrao.Trim();
        AtualizadaEm = DateTime.UtcNow;
    }
}
