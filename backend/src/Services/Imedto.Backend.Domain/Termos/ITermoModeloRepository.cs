namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Write-side de modelos (EF Core). Read-side fica em
/// <c>ITermoModeloQueryRepository</c> (Dapper).
/// </summary>
public interface ITermoModeloRepository
{
    /// <summary>
    /// Carrega um modelo do estabelecimento <paramref name="estabelecimentoId"/>.
    /// Retorna null se não existe, se é padrão do sistema ou se pertence a outro tenant.
    /// Inclui registros soft-deletados? <b>Não</b> — multi-tenant + ativos.
    /// </summary>
    Task<TermoModelo?> ObterPorIdDoEstabelecimentoOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Carrega um padrão do sistema (estabelecimento_id IS NULL). Retorna null se
    /// inexistente, deletado ou se pertence a um estabelecimento.
    /// </summary>
    Task<TermoModelo?> ObterPadraoDoSistemaPorIdOuNulo(long id);

    Task Salvar(TermoModelo modelo);
    Task SalvarVersao(TermoModeloVersao versao);
}
