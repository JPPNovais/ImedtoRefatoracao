using System.Linq;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using NUnit.Framework;

namespace Imedto.Backend.Test.Contracts.Vinculos;

/// <summary>
/// Bug #1 (LGPD) — o DTO publico de profissional NUNCA pode expor PII da
/// equipe (e-mail, modelo de permissao, datas de convite). Antes da correcao,
/// qualquer membro do tenant via essas informacoes ao acessar a lista de
/// profissionais usada por seletores de agenda/prontuario/orcamento.
///
/// Este teste e a "trava de regressao" — se alguem adicionar email/etc.
/// no DTO de novo, falha o build de testes.
/// </summary>
[TestFixture]
public class ProfissionalPublicoDtoTests
{
    private static readonly string[] CamposProibidosLgpd =
    {
        "Email",
        "ModeloPermissaoId",
        "ModeloPermissaoNome",
        "ConvidadoEm",
        "AceitoEm",
        "VinculoId",
        "Profissao", // a "profissao" do vinculo so vem em convites pendentes — fora do escopo publico
    };

    [Test]
    public void ProfissionalPublicoDto_NaoContemPii()
    {
        var props = typeof(ProfissionalPublicoDto).GetProperties().Select(p => p.Name).ToArray();

        foreach (var proibido in CamposProibidosLgpd)
        {
            Assert.That(props, Does.Not.Contain(proibido),
                $"Campo '{proibido}' nao deve estar no DTO publico — vazaria PII da equipe.");
        }
    }

    [Test]
    public void ProfissionalPublicoDto_ContemApenasCamposMinimos()
    {
        // Lista permitida: id estavel (UsuarioId), display (NomeCompleto + FotoUrl
        // para avatar nos seletores), contexto profissional (Especialidade,
        // Conselho), Status para o front distinguir Dono. Qualquer adicao aqui
        // exige justificativa explicita.
        //
        // FotoUrl e PII de baixo risco (mesma visibilidade que nome/especialidade
        // ja tinham) — exibida nos avatares de seletor/agenda/equipe.
        var esperados = new[]
        {
            "UsuarioId", "NomeCompleto", "Especialidade", "Conselho", "Status", "FotoUrl"
        };

        var props = typeof(ProfissionalPublicoDto).GetProperties().Select(p => p.Name).ToArray();

        Assert.That(props, Is.EquivalentTo(esperados),
            "DTO publico deve conter exatamente os campos minimos de seletor.");
    }
}
