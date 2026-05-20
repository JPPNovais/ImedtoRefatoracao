namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Write-side de termos emitidos (EF). Filtros multi-tenant obrigatórios em todos os métodos
/// exceto <see cref="ObterPorTokenOuNulo"/> (fluxo público anônimo).
/// </summary>
public interface ITermoEmitidoRepository
{
    Task<TermoEmitido?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Lookup anônimo por token público (fluxo de aceite via link). Não filtra por tenant —
    /// o token é segredo de 32 bytes (256 bits de entropia), impossível de adivinhar.
    /// Retorna apenas termos com <see cref="AssinaturaTipo.AceiteLink"/>.
    /// </summary>
    Task<TermoEmitido?> ObterPorTokenOuNulo(string tokenAceite);

    Task Salvar(TermoEmitido termo);
    Task SalvarAcessoLog(TermoEmitidoAcessoLog log);
}
