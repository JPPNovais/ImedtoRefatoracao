using Imedto.Backend.Application.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Infrastructure.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes de unidade do handler de PDF do prontuário.
/// Verificam: delegação ao IProntuarioPdfService, multi-tenant (prontuário de
/// outro tenant → 422 genérico), e que audit é responsabilidade do serviço.
/// </summary>
[TestFixture]
public class EmitirProntuarioPdfQueryHandlerTests
{
    private Mock<IProntuarioPdfService> _pdfService;
    private EmitirProntuarioPdfQueryHandler _sut;

    private const long PacienteId = 10L;
    private const long EstabelecimentoId = 1L;
    private const long OutroEstabId = 2L;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _pdfService = new Mock<IProntuarioPdfService>();
        _sut = new EmitirProntuarioPdfQueryHandler(_pdfService.Object);
    }

    private EmitirProntuarioPdfQuery Query(long estabId = EstabelecimentoId) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = estabId,
        SolicitanteUsuarioId = _solicitanteId,
    };

    [Test]
    public async Task Handle_ProntuarioEncontrado_RetornaBytes()
    {
        var bytesEsperados = new byte[] { 1, 2, 3 };
        _pdfService
            .Setup(s => s.GerarAsync(PacienteId, EstabelecimentoId, _solicitanteId))
            .ReturnsAsync(bytesEsperados);

        var resultado = await _sut.Handle(Query());

        Assert.That(resultado, Is.SameAs(bytesEsperados));
        _pdfService.Verify(s => s.GerarAsync(PacienteId, EstabelecimentoId, _solicitanteId), Times.Once);
    }

    [Test]
    public void Handle_ProntuarioCrossTenant_PropagaBusinessException()
    {
        // O IProntuarioPdfService é responsável por validar tenant e lançar BusinessException genérica.
        _pdfService
            .Setup(s => s.GerarAsync(PacienteId, OutroEstabId, _solicitanteId))
            .ThrowsAsync(new BusinessException("Prontuário não encontrado."));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Query(OutroEstabId)));

        Assert.That(ex.Message, Is.EqualTo("Prontuário não encontrado."),
            "Mensagem genérica — não deve revelar que o prontuário pertence a outro tenant.");
    }

    [Test]
    public async Task Handle_PropagaParametrosCorretosAoServico()
    {
        _pdfService
            .Setup(s => s.GerarAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<byte>());

        var query = Query();
        await _sut.Handle(query);

        // Garante que pacienteId, estabelecimentoId e solicitanteId chegam íntegros ao serviço.
        _pdfService.Verify(s => s.GerarAsync(
            query.PacienteId,
            query.EstabelecimentoId,
            query.SolicitanteUsuarioId), Times.Once);
    }
}
