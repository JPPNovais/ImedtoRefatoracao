using Microsoft.Extensions.Caching.Memory;

namespace Imedto.Backend.Application.Admin.Regioes;

/// <summary>
/// Rastreia todas as chaves de cache emitidas pelo handler de leitura do catálogo
/// de regiões e expõe um método para invalidar todas de uma vez.
///
/// Singleton: registrado junto ao handler de leitura (também singleton).
/// Os 5 handlers admin de mutação (scoped) recebem este singleton via DI e
/// chamam InvalidarTudo() após persistir — CA4: todas as variações de chave
/// (vista × ativas) são invalidadas em uma única chamada.
/// </summary>
public sealed class CatalogoRegioesCacheInvalidador
{
    private readonly IMemoryCache _cache;

    // Todos os valores possíveis dos dois eixos de variação da chave.
    // Construídos uma única vez: o conjunto é estável (os parâmetros não mudam em runtime).
    private static readonly IReadOnlyList<string> _todasAsChaves = GerarTodasAsChaves();

    public CatalogoRegioesCacheInvalidador(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>Invalida todas as variações de chave do catálogo de regiões.</summary>
    /// <remarks>
    /// Chave format: "catalogo:regioes:vista={vista}:ativas={ativas}"
    /// Variações: vista ∈ {all, anterior, posterior, circunferencial} × ativas ∈ {true, false} = 8 chaves.
    /// </remarks>
    public void InvalidarTudo()
    {
        foreach (var chave in _todasAsChaves)
            _cache.Remove(chave);
    }

    private static List<string> GerarTodasAsChaves()
    {
        // Espelha o formato exato do ListarRegioesCatalogoQueryHandlers:
        // $"catalogo:regioes:vista={query.Vista ?? "all"}:ativas={query.ApenasAtivas}"
        string[] vistas = ["all", "anterior", "posterior", "circunferencial"];
        bool[] ativas = [true, false];

        var chaves = new List<string>(vistas.Length * ativas.Length);
        foreach (var v in vistas)
            foreach (var a in ativas)
                chaves.Add($"catalogo:regioes:vista={v}:ativas={a}");

        return chaves;
    }
}
