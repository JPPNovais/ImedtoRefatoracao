using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Regioes;

namespace Imedto.Backend.Test.Application.Admin.Regioes;

/// <summary>
/// Testes do CatalogoRegioesCacheInvalidador — CA4 do briefing 2026-06-25_001.
/// Garante que TODAS as variações de chave (vista × ativas) são invalidadas de uma vez.
/// </summary>
[TestFixture]
public class CatalogoRegioesCacheInvalidadorTests
{
    private IMemoryCache _cache = null!;
    private CatalogoRegioesCacheInvalidador _invalidador = null!;

    [SetUp]
    public void Setup()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _invalidador = new CatalogoRegioesCacheInvalidador(_cache);
    }

    [TearDown]
    public void TearDown() => _cache.Dispose();

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private void PopularTodasAsChaves()
    {
        string[] vistas = ["all", "anterior", "posterior", "circunferencial"];
        bool[] ativas = [true, false];
        foreach (var v in vistas)
            foreach (var a in ativas)
                _cache.Set($"catalogo:regioes:vista={v}:ativas={a}", new object());
    }

    private bool CacheTemChave(string vista, bool ativas)
        => _cache.TryGetValue($"catalogo:regioes:vista={vista}:ativas={ativas}", out _);

    // ── CA4 — InvalidarTudo remove TODAS as 8 variações ─────────────────────────

    [Test]
    public void InvalidarTudo_RemoveTodasAsVariacoesDeChave()
    {
        PopularTodasAsChaves();

        _invalidador.InvalidarTudo();

        // Verifica as 4 vistas × 2 valores de ativas
        foreach (var vista in new[] { "all", "anterior", "posterior", "circunferencial" })
            foreach (var ativas in new[] { true, false })
                Assert.That(CacheTemChave(vista, ativas), Is.False,
                    $"Cache ainda contém: vista={vista}:ativas={ativas}");
    }

    [Test]
    public void InvalidarTudo_NaoMantenhNenhumaVariacao()
    {
        PopularTodasAsChaves();
        _invalidador.InvalidarTudo();

        // Garante que a contagem exata de hits é zero (não só sample check)
        var chaves = new[] { "all", "anterior", "posterior", "circunferencial" };
        var contagem = chaves.Sum(v =>
            (CacheTemChave(v, true) ? 1 : 0) + (CacheTemChave(v, false) ? 1 : 0));

        Assert.That(contagem, Is.Zero, "Alguma chave de cache sobreviveu à invalidação.");
    }

    [Test]
    public void InvalidarTudo_CacheTotalmente_Vazio_NaoLancaExcecao()
    {
        // Cache vazio: não deve lançar exceção ao tentar remover chaves inexistentes
        Assert.DoesNotThrow(() => _invalidador.InvalidarTudo());
    }

    [Test]
    public void InvalidarTudo_AposPreencher_TodasAsChavesAusentes()
    {
        // Este teste verifica apenas que TODAS as chaves foram removidas (sem re-inserir).
        // Comportamento de re-inserção é garantido pelo MemoryCache da BCL — testado
        // em isolamento no diagnóstico; não precisa repetição aqui.
        PopularTodasAsChaves();
        _invalidador.InvalidarTudo();

        // Confirma que nenhuma das 8 chaves sobrou
        foreach (var vista in new[] { "all", "anterior", "posterior", "circunferencial" })
            foreach (var ativas in new[] { true, false })
                Assert.That(CacheTemChave(vista, ativas), Is.False,
                    $"Chave vista={vista}:ativas={ativas} deveria ter sido removida.");
    }
}
