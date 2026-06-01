namespace Imedto.Backend.Domain.Receitas;

public interface IReceitaRepository
{
    /// <summary>
    /// Carrega a receita (com itens) filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<Receita?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Carrega a receita SEM filtro de tenant. Uso exclusivo do handler de webhook de
    /// assinatura digital — o callback externo não carrega claim de tenant; a
    /// autenticidade é garantida pelo HMAC do provedor, não pelo usuário.
    /// </summary>
    Task<Receita?> ObterSemTenantAsync(long id);

    Task Salvar(Receita receita);

    /// <summary>
    /// Lista receitas em <see cref="StatusAssinaturaDigital.AssinaturaPendente"/> cuja
    /// <c>assinatura_solicitada_em</c> é anterior a <paramref name="anteriorA"/>.
    /// Usado pelo job de expiração periódica.
    /// </summary>
    Task<List<Receita>> ListarPendentesParaExpirarAsync(DateTime anteriorA, CancellationToken ct = default);
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
