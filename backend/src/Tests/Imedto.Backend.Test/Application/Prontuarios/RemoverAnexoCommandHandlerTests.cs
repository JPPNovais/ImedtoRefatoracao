using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes do RemoverAnexoCommandHandler — briefing 2026-06-27_002 (R8/CA8).
/// Garante soft-delete, gating autor-ou-dono e mensagem genérica.
/// </summary>
[TestFixture]
public class RemoverAnexoCommandHandlerTests
{
    private Mock<IProntuarioAnexoRepository> _repo;
    private RemoverAnexoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private static readonly Guid AutorId = Guid.NewGuid();
    private static readonly Guid OutroProfId = Guid.NewGuid();
    private static readonly Guid DonoId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProntuarioAnexoRepository>();
        _sut = new RemoverAnexoCommandHandler(_repo.Object);
    }

    private static ProntuarioAnexo AnexoDoAutor(long id = 1)
    {
        var a = ProntuarioAnexo.Registrar(
            prontuarioId: 100,
            estabelecimentoId: EstabelecimentoId,
            evolucaoId: null,
            storagePath: "est_1/paciente_50/uuid_foto.jpg",
            nomeOriginal: "foto.jpg",
            mimeType: "image/jpeg",
            tamanhoBytes: 512_000,
            criadoPorUsuarioId: AutorId,
            marcador: "foto-paciente");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(a, id);
        return a;
    }

    [Test]
    public async Task Handle_AutorRemoveProprio_MarcaComoDeletado()
    {
        var anexo = AnexoDoAutor();
        _repo.Setup(r => r.ObterPorIdOuNulo(1, EstabelecimentoId)).ReturnsAsync(anexo);

        await _sut.Handle(new RemoverAnexoCommand
        {
            AnexoId = 1,
            PacienteId = 50,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = AutorId,
            SolicitantePapel = TenantPapel.Profissional,
        });

        Assert.That(anexo.DeletadoEm, Is.Not.Null, "CA8: soft-delete deve preencher deletado_em.");
        _repo.Verify(r => r.Salvar(anexo), Times.Once);
    }

    [Test]
    public async Task Handle_DonoRemoveQualquer_MarcaComoDeletado()
    {
        // CA5 / R8: Dono pode remover anexo de qualquer profissional.
        var anexo = AnexoDoAutor();
        _repo.Setup(r => r.ObterPorIdOuNulo(1, EstabelecimentoId)).ReturnsAsync(anexo);

        await _sut.Handle(new RemoverAnexoCommand
        {
            AnexoId = 1,
            PacienteId = 50,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DonoId,
            SolicitantePapel = TenantPapel.Dono,
        });

        Assert.That(anexo.DeletadoEm, Is.Not.Null, "Dono deve conseguir remover qualquer anexo.");
        _repo.Verify(r => r.Salvar(anexo), Times.Once);
    }

    [Test]
    public void Handle_ProfissionalNaoAutor_LancaMensagemGenerica()
    {
        // CA11/R8: Dr.B não pode remover anexo do Dr.A — mensagem "não encontrado" genérica.
        var anexo = AnexoDoAutor();
        _repo.Setup(r => r.ObterPorIdOuNulo(1, EstabelecimentoId)).ReturnsAsync(anexo);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RemoverAnexoCommand
        {
            AnexoId = 1,
            PacienteId = 50,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = OutroProfId,
            SolicitantePapel = TenantPapel.Profissional,
        }));

        Assert.That(ex.Message, Is.EqualTo("Anexo não encontrado."),
            "Mensagem deve ser genérica — nunca revelar que o anexo pertence a colega.");
        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioAnexo>()), Times.Never);
    }

    [Test]
    public void Handle_AnexoDeOutroTenant_LancaMensagemGenerica()
    {
        // CA17: anexo de outro tenant → ObterPorIdOuNulo retorna null (filtro multi-tenant no repo).
        _repo.Setup(r => r.ObterPorIdOuNulo(1, EstabelecimentoId)).ReturnsAsync((ProntuarioAnexo?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RemoverAnexoCommand
        {
            AnexoId = 1,
            PacienteId = 50,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = AutorId,
            SolicitantePapel = TenantPapel.Profissional,
        }));

        Assert.That(ex.Message, Is.EqualTo("Anexo não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioAnexo>()), Times.Never);
    }

    [Test]
    public void Handle_AnexoJaDeletado_LancaBusinessException()
    {
        var anexo = AnexoDoAutor();
        anexo.MarcarComoDeletado(AutorId); // já deletado
        _repo.Setup(r => r.ObterPorIdOuNulo(1, EstabelecimentoId)).ReturnsAsync(anexo);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RemoverAnexoCommand
        {
            AnexoId = 1,
            PacienteId = 50,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = AutorId,
            SolicitantePapel = TenantPapel.Profissional,
        }));

        Assert.That(ex.Message, Does.Contain("já foi removido"));
    }
}
