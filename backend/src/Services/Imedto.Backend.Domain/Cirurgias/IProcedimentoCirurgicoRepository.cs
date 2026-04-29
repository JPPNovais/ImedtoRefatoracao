namespace Imedto.Backend.Domain.Cirurgias;

/// <summary>
/// Repositório de escrita do aggregate <see cref="ProcedimentoCirurgico"/>.
/// As leituras de listagem usam <c>IProcedimentoCirurgicoQueryRepository</c> (Dapper).
/// </summary>
public interface IProcedimentoCirurgicoRepository
{
    /// <summary>Carrega o aggregate completo (com a equipe) — falha se não existir.</summary>
    Task<ProcedimentoCirurgico> ObterPorId(long id);
    Task<ProcedimentoCirurgico?> ObterPorIdOuNulo(long id);
    Task Salvar(ProcedimentoCirurgico procedimento);
}
