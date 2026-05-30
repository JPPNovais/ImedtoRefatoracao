namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Leitor de configurações globais com cache em memória (TTL 60s).
/// Injetado por handlers que precisam de parâmetros do sistema (ex: dias de trial, tempo de sessão).
///
/// Regra: falha ao ler → retorna defaultValue sem lançar exceção (fallback seguro).
/// Regra: após Atualizar via <see cref="AtualizarConfigAdminCommandHandler"/>, invalidar
/// o cache chamando <see cref="InvalidarCache"/>.
/// </summary>
public interface IConfigGlobalReader
{
    Task<int> LerInt(string chave, int defaultValue, CancellationToken ct = default);
    Task<string> LerString(string chave, string defaultValue, CancellationToken ct = default);
    Task<bool> LerBool(string chave, bool defaultValue, CancellationToken ct = default);

    /// <summary>Remove a entrada do cache para forçar releitura na próxima chamada.</summary>
    void InvalidarCache(string chave);
}
