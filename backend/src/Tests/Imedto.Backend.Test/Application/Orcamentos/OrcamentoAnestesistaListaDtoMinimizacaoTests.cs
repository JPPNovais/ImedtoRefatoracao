using System.Reflection;
using System.Text.Json;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

/// <summary>
/// Guarda-corpo de LGPD: a listagem de anestesistas
/// (GET /orcamentos/configuracoes/anestesistas) NÃO pode expor telefone — a tela
/// de listagem não exibe esse campo. Já o detalhe (GET /anestesistas/{id})
/// precisa continuar expondo telefone, pois o drawer de edição usa.
/// </summary>
[TestFixture]
public class OrcamentoAnestesistaListaDtoMinimizacaoTests
{
    [Test]
    public void Lista_NaoExpoeTelefone()
    {
        var props = typeof(OrcamentoAnestesistaListaDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.That(props.Contains("Telefone"), Is.False,
            "Minimização LGPD: OrcamentoAnestesistaListaDto NÃO pode expor 'Telefone' — " +
            "a tela de listagem não exibe telefone. Use OrcamentoAnestesistaDto no detalhe.");
    }

    [Test]
    public void Lista_SerializacaoJson_NaoExpoeTelefone()
    {
        var dto = new OrcamentoAnestesistaListaDto
        {
            Id = 1,
            EstabelecimentoId = 1,
            Nome = "Dr. Teste",
            Crm = "12345",
            Especialidade = "Anestesiologia",
            TabelaHonorarios = "Padrão",
            Ativo = true,
            CriadaEm = System.DateTime.UtcNow,
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        using var doc = JsonDocument.Parse(json);
        var props = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.That(props, Does.Not.Contain("telefone"),
            "O JSON da listagem NÃO pode conter 'telefone' — vaza PII.");
    }

    [Test]
    public void Detalhe_ContinuaExpondoTelefone()
    {
        // DTO de detalhe (drawer de edição) precisa ter telefone — o usuário edita.
        var props = typeof(OrcamentoAnestesistaDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.That(props.Contains("Telefone"), Is.True,
            "OrcamentoAnestesistaDto (detalhe) DEVE expor 'Telefone' para o drawer de edição.");
    }
}
