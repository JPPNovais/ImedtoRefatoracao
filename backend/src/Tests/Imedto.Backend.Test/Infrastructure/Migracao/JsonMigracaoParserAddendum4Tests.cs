using System.Text;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Infrastructure.Migracao;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Migracao;

/// <summary>
/// Testes do JsonMigracaoParser — addendum 4 (CA70-72, CA71, CA80-81).
/// </summary>
[TestFixture]
public class JsonMigracaoParserAddendum4Tests
{
    private static Stream ToStream(string conteudo) =>
        new MemoryStream(Encoding.UTF8.GetBytes(conteudo));

    // ─── CA70 — Dump aninhado vira N blocos ──────────────────────────────────

    [Test]
    public async Task ParsearAsync_DumpAninhado_CadaArrayViaBlocoCandidato()
    {
        const string json = """
            {
                "estabelecimento": { "nome": "Clinica X", "cnpj": "00.000.000/0001-00" },
                "reparticoes": [{ "id": 1, "nome": "Sala A" }],
                "pacientes": [{ "nome": "Joao", "cpf": "000.000.000-00" }],
                "agendamentos": [{ "data": "2024-01-01", "paciente": "Joao" }, { "data": "2024-01-02", "paciente": "Maria" }]
            }
            """;

        var parser = new JsonMigracaoParser();
        var result = await parser.ParsearAsync(ToStream(json), "dump_sistema.json");

        // 4 blocos: 3 arrays + 1 objeto de config.
        Assert.That(result.Blocos, Has.Count.EqualTo(4), "Esperados 4 blocos (3 arrays + 1 config).");

        var blocoConfig = result.Blocos.FirstOrDefault(b => b.NomeBloco == "estabelecimento");
        Assert.That(blocoConfig, Is.Not.Null, "Objeto de config deve virar bloco.");
        Assert.That(blocoConfig!.EhConfig, Is.True, "estabelecimento{} deve ser EhConfig=true.");
        Assert.That(blocoConfig.Linhas, Is.Empty, "Bloco de config não tem linhas.");

        var blocoPacientes = result.Blocos.FirstOrDefault(b => b.NomeBloco == "pacientes");
        Assert.That(blocoPacientes, Is.Not.Null);
        Assert.That(blocoPacientes!.EhConfig, Is.False);
        Assert.That(blocoPacientes.Linhas, Has.Count.EqualTo(1));
        Assert.That(blocoPacientes.Cabecalhos, Does.Contain("nome"));
        Assert.That(blocoPacientes.Cabecalhos, Does.Contain("cpf"));

        var blocoAgendamentos = result.Blocos.FirstOrDefault(b => b.NomeBloco == "agendamentos");
        Assert.That(blocoAgendamentos, Is.Not.Null);
        Assert.That(blocoAgendamentos!.Linhas, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ParsearAsync_DumpAninhado_NenhumArrayDescartado()
    {
        // Corrige o bug do EncontrarPrimeiroArray — antes só lia o primeiro array.
        const string json = """
            {
                "profissionais": [{ "nome": "Dr Ana" }],
                "pacientes": [{ "nome": "Joao" }],
                "prontuarios": [{ "tipo": "consulta" }]
            }
            """;

        var parser = new JsonMigracaoParser();
        var result = await parser.ParsearAsync(ToStream(json), "dump.json");

        Assert.That(result.Blocos, Has.Count.EqualTo(3), "Os 3 arrays devem ser capturados.");
        Assert.That(result.Blocos.Select(b => b.NomeBloco),
            Is.EquivalentTo(new[] { "profissionais", "pacientes", "prontuarios" }),
            "Nenhum array deve ser descartado (corrige EncontrarPrimeiroArray).");
    }

    // ─── CA71 — JSON-array na raiz: 1 bloco, sem regressão ──────────────────

    [Test]
    public async Task ParsearAsync_JsonArrayNaRaiz_1BlocoSemRegressao()
    {
        const string json = """[{"nome":"Joao","email":"j@a.com"},{"nome":"Maria","email":"m@b.com"}]""";

        var parser = new JsonMigracaoParser();
        var result = await parser.ParsearAsync(ToStream(json), "pacientes.json");

        // 1 bloco — compatibilidade preservada (CA71).
        Assert.That(result.Blocos, Has.Count.EqualTo(1));
        Assert.That(result.Blocos[0].NomeBloco, Is.EqualTo("pacientes"), "Nome do bloco = nome do arquivo sem extensão.");
        Assert.That(result.Blocos[0].EhConfig, Is.False);
        Assert.That(result.Blocos[0].Linhas, Has.Count.EqualTo(2));

        // Cabecalhos e Linhas do ArquivoParseado continuam funcionando (compatibilidade legada).
        Assert.That(result.Cabecalhos, Does.Contain("nome"));
        Assert.That(result.Linhas, Has.Count.EqualTo(2));
    }

    // ─── CA72 — Sub-objeto não mapeado, não inventado (D-S4/R-S2) ──────────

    [Test]
    public async Task ParsearAsync_RegistroComSubObjeto_NaoIncluiSubObjetoNosCabecalhos()
    {
        const string json = """
            {
                "pacientes": [
                    {
                        "nome": "Joao",
                        "data_nascimento": "1990-01-01",
                        "campos_especificos": { "alergia": "penicilina", "tipo_sanguineo": "A+" },
                        "ids_relacionados": [10, 20, 30]
                    }
                ]
            }
            """;

        var parser = new JsonMigracaoParser();
        var result = await parser.ParsearAsync(ToStream(json), "dump.json");

        var bloco = result.Blocos.First(b => b.NomeBloco == "pacientes");

        // Campos planos incluídos.
        Assert.That(bloco.Cabecalhos, Does.Contain("nome"), "Campo plano deve estar nos cabeçalhos.");
        Assert.That(bloco.Cabecalhos, Does.Contain("data_nascimento"), "Campo plano deve estar nos cabeçalhos.");

        // Sub-objeto e array-interno excluídos (D-S4/CA72).
        Assert.That(bloco.Cabecalhos, Does.Not.Contain("campos_especificos"),
            "Sub-objeto não deve virar campo mapeável (D-S4).");
        Assert.That(bloco.Cabecalhos, Does.Not.Contain("ids_relacionados"),
            "Array interno não deve virar campo mapeável (D-S4).");

        // A linha não contém esses campos.
        var linha = bloco.Linhas[0];
        Assert.That(linha.ContainsKey("campos_especificos"), Is.False,
            "Sub-objeto não deve aparecer como campo na linha.");
    }

    // ─── CA80 — Mojibake UTF-8↔Latin-1 corrigido ────────────────────────────

    [Test]
    public async Task ParsearAsync_MojibakeUtf8Latin1_ValorCorrigido()
    {
        // "Cirurgia Plástica" codificado como UTF-8 lido como Latin-1 → mojibake "Cirurgia PlÃ¡stica".
        // O JSON é construído com concatenação simples para evitar ambiguidade de interpolação.
        const string valorMojibake = "Cirurgia PlÃ¡stica"; // Ã + ¡ (U+00A1) = mojibake de á
        var json = "{\"especialidades\": [{\"nome\": \"" + valorMojibake + "\"}]}";

        var parser = new JsonMigracaoParser();
        var result = await parser.ParsearAsync(ToStream(json), "dump.json");

        var bloco = result.Blocos.First(b => b.NomeBloco == "especialidades");
        var nomeCorrigido = bloco.Linhas[0]["nome"];

        // Após normalização, deve ser "Cirurgia Plástica" — mojibake corrigido na ingestão (CA80).
        Assert.That(nomeCorrigido, Is.EqualTo("Cirurgia Plástica"),
            "Mojibake deve ser corrigido na ingestão (CA80).");
    }

    // ─── CA81 — Encoding ambíguo é sinalizado, não corrompido ───────────────

    [Test]
    public async Task ParsearAsync_TextoPuroAscii_NaoCorrupto_NaoSuspeitado()
    {
        const string json = """{"dados":[{"nome":"Joao","email":"joao@test.com"}]}""";

        var parser = new JsonMigracaoParser();
        var result = await parser.ParsearAsync(ToStream(json), "dump.json");

        var bloco = result.Blocos.First();
        // Texto ASCII puro → sem suspeita de encoding.
        Assert.That(bloco.EncodingSuspeito, Is.False, "Texto ASCII puro não deve ser marcado como suspeito.");
        Assert.That(bloco.Linhas[0]["nome"], Is.EqualTo("Joao"), "Texto correto não deve ser alterado.");
    }
}
