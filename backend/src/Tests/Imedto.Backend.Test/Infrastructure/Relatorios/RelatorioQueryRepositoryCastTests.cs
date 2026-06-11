using System.IO;
using System.Reflection;
using Imedto.Backend.Contracts.Relatorios;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Relatorios;

/// <summary>
/// Trava de regressão para o bug 500 de cast em relatórios (hotfix 2026-06-11).
///
/// Postgres retorna COUNT(*) como int8 (bigint). Dapper falha ao materializar
/// bigint em C# int sem o cast explícito ::int no SQL. Três DTOs foram afetados:
///   - AgendamentosPorStatusDto.Quantidade
///   - AgendamentosPorDiaDto.Quantidade
///   - FaturamentoCategoriaDto.Quantidade
///
/// Este teste:
///   1. Confirma que as propriedades Quantidade são do tipo int (não long).
///   2. Confirma que o SQL do repositório usa COUNT(*)::int nas 3 ocorrências
///      em que o resultado é materializado para esses DTOs.
///
/// Se alguém trocar int por long nos DTOs ou remover o cast ::int do SQL,
/// o teste falha antes de chegar em produção.
/// </summary>
[TestFixture]
public class RelatorioQueryRepositoryCastTests
{
    // ── Contrato de DTO ────────────────────────────────────────────────────────

    [Test]
    public void AgendamentosPorStatusDto_Quantidade_DeveSerInt()
    {
        var prop = typeof(AgendamentosPorStatusDto).GetProperty("Quantidade");
        Assert.That(prop, Is.Not.Null, "Propriedade Quantidade não encontrada em AgendamentosPorStatusDto.");
        Assert.That(prop!.PropertyType, Is.EqualTo(typeof(int)),
            "AgendamentosPorStatusDto.Quantidade deve ser int (não long/bigint) — " +
            "Dapper requer cast ::int no SQL para materializar corretamente.");
    }

    [Test]
    public void AgendamentosPorDiaDto_Quantidade_DeveSerInt()
    {
        var prop = typeof(AgendamentosPorDiaDto).GetProperty("Quantidade");
        Assert.That(prop, Is.Not.Null, "Propriedade Quantidade não encontrada em AgendamentosPorDiaDto.");
        Assert.That(prop!.PropertyType, Is.EqualTo(typeof(int)),
            "AgendamentosPorDiaDto.Quantidade deve ser int (não long/bigint).");
    }

    [Test]
    public void FaturamentoCategoriaDto_Quantidade_DeveSerInt()
    {
        var prop = typeof(FaturamentoCategoriaDto).GetProperty("Quantidade");
        Assert.That(prop, Is.Not.Null, "Propriedade Quantidade não encontrada em FaturamentoCategoriaDto.");
        Assert.That(prop!.PropertyType, Is.EqualTo(typeof(int)),
            "FaturamentoCategoriaDto.Quantidade deve ser int (não long/bigint).");
    }

    // ── Regressão de SQL — cast ::int obrigatório ──────────────────────────────

    [Test]
    public void RelatorioQueryRepository_ContemCastIntNasTresOcorrenciasDeQuantidade()
    {
        // Localiza o arquivo relativo ao assembly de teste — funciona tanto em
        // desenvolvimento local quanto em CI (o .cs está na solução, o caminho
        // é derivado do assembly de infraestrutura como âncora).
        var infraAssembly = typeof(Imedto.Backend.Infrastructure.AppReadConnectionString).Assembly;
        var infraDir = Path.GetDirectoryName(infraAssembly.Location)!;

        // Sobe a árvore até encontrar o diretório raiz da solução (contém Imedto.Backend.sln).
        var candidato = new DirectoryInfo(infraDir);
        while (candidato != null && !File.Exists(Path.Combine(candidato.FullName, "Imedto.Backend.sln")))
            candidato = candidato.Parent;

        Assert.That(candidato, Is.Not.Null, "Não foi possível localizar o diretório raiz da solução.");

        // .sln está em backend/src/ — o repositório de produção é um nível abaixo.
        var caminho = Path.Combine(
            candidato!.FullName,
            "Services", "Imedto.Backend.Infrastructure",
            "Database", "Repositories", "RelatorioQueryRepository.cs");

        Assert.That(File.Exists(caminho), Is.True,
            $"Arquivo RelatorioQueryRepository.cs não encontrado em: {caminho}");

        var conteudo = File.ReadAllText(caminho);

        // As 3 queries que materializam DTOs com int Quantidade devem usar COUNT(*)::int.
        // Verificamos que o padrão incorreto não existe.
        Assert.That(conteudo, Does.Not.Contain("COUNT(*) AS Quantidade"),
            "COUNT(*) sem ::int encontrado no RelatorioQueryRepository — " +
            "Postgres retorna bigint, Dapper falha ao materializar em int. " +
            "Use COUNT(*)::int AS Quantidade.");

        // Verificamos que o padrão correto existe pelo menos 3 vezes.
        // O padrão usa regex-free: buscamos "COUNT(*)::int" (pode ter espaços de
        // alinhamento antes de "AS Quantidade") — contamos ocorrências de "COUNT(*)::int".
        var ocorrencias = CountOcorrencias(conteudo, "COUNT(*)::int");
        Assert.That(ocorrencias, Is.GreaterThanOrEqualTo(3),
            $"Esperado pelo menos 3 ocorrências de 'COUNT(*)::int', " +
            $"encontrado: {ocorrencias}. " +
            "Confirme que RelatorioFaturamento, RelatorioAgendamentos (status e por dia) " +
            "usam o cast ::int.");
    }

    private static int CountOcorrencias(string texto, string padrao)
    {
        int count = 0;
        int pos = 0;
        while ((pos = texto.IndexOf(padrao, pos, StringComparison.Ordinal)) >= 0)
        {
            count++;
            pos += padrao.Length;
        }
        return count;
    }
}
