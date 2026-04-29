namespace Imedto.Backend.Domain.Receitas;

public interface IReceitaRepository
{
    Task<Receita> ObterPorId(long id);
    Task<Receita?> ObterPorIdOuNulo(long id);
    Task Salvar(Receita receita);
}

public interface IConfiguracaoReceitaRepository
{
    Task<ConfiguracaoReceitaEstabelecimento?> ObterPorEstabelecimentoOuNulo(long estabelecimentoId);
    Task Salvar(ConfiguracaoReceitaEstabelecimento configuracao);
}

public interface IMedicamentoFavoritoRepository
{
    /// <summary>
    /// Procura favorito existente por chave única (profissional, estabelecimento, medicamento, posologia).
    /// Se existir → incrementa o uso; se não → cria com count=1.
    /// </summary>
    Task RegistrarUso(
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        string medicamento,
        string? posologia,
        ViaAdministracao? via);
}
