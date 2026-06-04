using System.Text.Json;
using Imedto.Backend.API.Controllers;
using NUnit.Framework;

namespace Imedto.Backend.IntegrationTest.Infrastructure;

/// <summary>
/// Regressão do bug onde o controller não mapeava TipoPrazoEntrega do DTO ao command,
/// fazendo o backend sempre persistir "corridos" mesmo quando o frontend enviava "uteis".
///
/// Valida o path: JSON recebido pelo controller → record FornecedorPayloadDto → campo TipoPrazoEntrega.
/// </summary>
[TestFixture]
public class FornecedorPayloadDtoMapeamentoTests
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [Test]
    public void FornecedorPayloadDto_DeserializaComTipoPrazoUteis_CampoPreservado()
    {
        var json = """
            {
                "razaoSocial": "Distribuidora X",
                "prazoEntregaDias": 7,
                "tipoPrazoEntrega": "uteis"
            }
            """;

        var dto = JsonSerializer.Deserialize<FornecedorPayloadDto>(json, JsonOpts);

        Assert.That(dto!.TipoPrazoEntrega, Is.EqualTo("uteis"),
            "TipoPrazoEntrega 'uteis' enviado pelo frontend deve chegar ao record DTO sem ser sobrescrito pelo default 'corridos'.");
    }

    [Test]
    public void FornecedorPayloadDto_SemTipoPrazo_DefaultECorridos()
    {
        var json = """
            {
                "razaoSocial": "Distribuidora Y",
                "prazoEntregaDias": 5
            }
            """;

        var dto = JsonSerializer.Deserialize<FornecedorPayloadDto>(json, JsonOpts);

        Assert.That(dto!.TipoPrazoEntrega, Is.EqualTo("corridos"),
            "Quando o campo não é enviado, o default 'corridos' deve ser mantido.");
    }
}
