using System.Reflection;
using System.Text.Json;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Pacientes;

/// <summary>
/// Guarda-corpo de LGPD (Correção 5): o DTO de autocomplete de paciente
/// é a fonte da minimização. Qualquer campo extra (CPF, telefone, data
/// nascimento, e-mail, endereço) adicionado aqui vaza PII no seletor.
///
/// Estes testes falham na intenção: se alguém adicionar uma propriedade
/// PII no DTO, o teste de superfície quebra e força revisão.
/// </summary>
[TestFixture]
public class PacienteBuscaRapidaDtoMinimizacaoTests
{
    [Test]
    public void Dto_TemApenasIdENomeCompleto()
    {
        var props = typeof(PacienteBuscaRapidaDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.That(props, Is.EquivalentTo(new[] { "Id", "NomeCompleto" }),
            "Minimização LGPD: o DTO de busca rápida NÃO pode incluir CPF, telefone, " +
            "data de nascimento, e-mail, endereço ou qualquer outro dado pessoal além " +
            "do necessário para o seletor (nome + id).");
    }

    [Test]
    public void Dto_NaoExpoeCamposPii()
    {
        var nomesProibidos = new[]
        {
            "Cpf", "Telefone", "Email", "Endereco", "DataNascimento",
            "DocumentoInternacional", "Genero", "Observacoes", "Convenio",
        };

        var props = typeof(PacienteBuscaRapidaDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var proibido in nomesProibidos)
        {
            Assert.That(props.Contains(proibido), Is.False,
                $"PacienteBuscaRapidaDto NÃO pode expor '{proibido}' — fere a minimização LGPD.");
        }
    }

    [Test]
    public void Serializacao_JsonSoTemIdENome()
    {
        // Defesa adicional: o JSON enviado ao cliente só pode conter id + nomeCompleto.
        // Se alguém adicionar uma prop nova com [JsonIgnore], esse teste ainda passa —
        // o que está correto, pois o que importa é o que sai na resposta HTTP.
        var dto = new PacienteBuscaRapidaDto
        {
            Id = 42,
            NomeCompleto = "Maria Souza",
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        using var doc = JsonDocument.Parse(json);
        var props = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.That(props, Is.EquivalentTo(new[] { "id", "nomeCompleto" }));
    }
}
