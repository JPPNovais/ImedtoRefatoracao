namespace Imedto.Backend.Domain.Cirurgias;

/// <summary>
/// Repositório de escrita do aggregate <see cref="ProcedimentoCirurgico"/>.
/// As leituras de listagem usam <c>IProcedimentoCirurgicoQueryRepository</c> (Dapper).
/// </summary>
public interface IProcedimentoCirurgicoRepository
{
    /// <summary>
    /// Carrega o aggregate completo (com a equipe) filtrando por
    /// <paramref name="estabelecimentoId"/> (defense-in-depth IDOR/LGPD).
    /// Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<ProcedimentoCirurgico?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task Salvar(ProcedimentoCirurgico procedimento);
}
