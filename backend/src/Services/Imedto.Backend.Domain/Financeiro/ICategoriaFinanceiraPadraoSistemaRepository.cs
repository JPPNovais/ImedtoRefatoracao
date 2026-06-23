namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Repositório de escrita para o catálogo global de categorias financeiras padrão.
/// Sem EstabelecimentoId — escopo plataforma, acesso exclusivo pelo admin global.
/// Briefing 2026-06-22_003 — M2/M3.
/// </summary>
public interface ICategoriaFinanceiraPadraoSistemaRepository
{
    Task<CategoriaFinanceiraPadraoSistema?> ObterPorIdOuNulo(long id);

    /// <summary>Verifica unicidade (nome + tipo) no catálogo global antes do INSERT.</summary>
    Task<bool> ExisteGlobalComNomeETipo(string nome, TipoCategoria tipo, CancellationToken ct = default);

    /// <summary>Lista todas as categorias padrão ativas do catálogo global (para seed por estabelecimento).</summary>
    Task<IReadOnlyList<CategoriaFinanceiraPadraoSistema>> ListarAtivas(CancellationToken ct = default);

    Task Salvar(CategoriaFinanceiraPadraoSistema categoria, CancellationToken ct = default);
}
