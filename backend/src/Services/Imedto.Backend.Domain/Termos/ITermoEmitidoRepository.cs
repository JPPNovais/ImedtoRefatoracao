namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Write-side de termos emitidos (EF). Filtro multi-tenant obrigatório em todos os métodos.
/// O fluxo público anônimo (AceiteLink) foi removido no briefing 2026-06-12_002.
/// </summary>
public interface ITermoEmitidoRepository
{
    Task<TermoEmitido?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(TermoEmitido termo);
}
