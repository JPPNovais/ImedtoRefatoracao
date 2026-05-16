using System.Linq;
using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using NUnit.Framework;

namespace Imedto.Backend.Test.Contracts.Estabelecimentos;

/// <summary>
/// Trava de regressao (LGPD — minimizacao). O DTO de leitura de estabelecimento
/// e devolvido em GET /auth/bootstrap e GET /estabelecimento, expondo o payload
/// para qualquer membro do tenant. Campos que nao tem uso na UI nao devem voltar
/// — em especial o `DonoUsuarioId` (Guid auth interno do proprietario).
///
/// Se alguem reintroduzir o campo, este teste falha o build.
/// </summary>
[TestFixture]
public class EstabelecimentoDtoTests
{
    private static readonly string[] CamposProibidosLgpd =
    {
        "DonoUsuarioId",
    };

    [Test]
    public void EstabelecimentoDto_NaoExpoeIdInternoDoDono()
    {
        var props = typeof(EstabelecimentoDto).GetProperties().Select(p => p.Name).ToArray();

        foreach (var proibido in CamposProibidosLgpd)
        {
            Assert.That(props, Does.Not.Contain(proibido),
                $"Campo '{proibido}' nao deve estar no DTO publico — vazaria id auth interno do dono (LGPD).");
        }
    }

    [Test]
    public void EstabelecimentoDto_ExpoePapelDoUsuario()
    {
        // PapelDoUsuario e o substituto legitimo de DonoUsuarioId: o front so
        // precisa saber se o usuario corrente e Dono ou Profissional naquele
        // estabelecimento, nao o Guid do dono.
        var props = typeof(EstabelecimentoDto).GetProperties().Select(p => p.Name).ToArray();
        Assert.That(props, Does.Contain("PapelDoUsuario"));
    }
}
