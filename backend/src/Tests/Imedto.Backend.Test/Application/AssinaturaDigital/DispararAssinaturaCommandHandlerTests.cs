using System.Collections.Generic;
using Imedto.Backend.Application.AssinaturaDigital.Commands;
using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.AssinaturaDigital;

[TestFixture]
public class DispararAssinaturaCommandHandlerTests
{
    private Mock<IReceitaRepository> _receitaRepo = null!;
    private Mock<IAssinaturaCertificadoRepository> _certRepo = null!;
    private Mock<IAssinaturaDigitalProvider> _provider = null!;
    private Mock<IAssinaturaAuditLogRepository> _auditRepo = null!;
    private DispararAssinaturaCommandHandler _sut = null!;
    private EphemeralDataProtectionProvider _dataProtection = null!;

    private readonly Guid _prescritoreId = Guid.NewGuid();
    private readonly Guid _outroMedicoId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long ReceitaId = 100;

    [SetUp]
    public void SetUp()
    {
        _receitaRepo = new Mock<IReceitaRepository>();
        _certRepo = new Mock<IAssinaturaCertificadoRepository>();
        _provider = new Mock<IAssinaturaDigitalProvider>();
        _auditRepo = new Mock<IAssinaturaAuditLogRepository>();
        _dataProtection = new EphemeralDataProtectionProvider();

        _sut = new DispararAssinaturaCommandHandler(
            _receitaRepo.Object,
            _certRepo.Object,
            _provider.Object,
            _auditRepo.Object,
            _dataProtection,
            NullLogger<DispararAssinaturaCommandHandler>.Instance);
    }

    private Receita ReceitaPrescritorValido()
    {
        var itens = new List<(string, string, string?, ViaAdministracao?, string?)> { ("Dipirona", "1 cp", null, null, null) };
        var r = Receita.Emitir(1, 2, _prescritoreId, EstabelecimentoId, TipoReceita.Comum, null, null, itens);
        typeof(Imedto.Backend.SharedKernel.Domain.Entity).GetProperty(nameof(Imedto.Backend.SharedKernel.Domain.Entity.Id))!.SetValue(r, ReceitaId);
        return r;
    }

    private AssinaturaCertificado CertificadoValido()
    {
        // Usa o mesmo EphemeralDataProtectionProvider do SUT para compatibilidade de Protect/Unprotect.
        var protector = _dataProtection.CreateProtector("assinatura.refresh_token");
        return AssinaturaCertificado.Vincular(_prescritoreId, "BirdId", protector.Protect("token-real"), null);
    }

    // CA-03: somente o prescritor pode assinar.
    [Test]
    public void Handle_OutroMedico_LancaBusinessException()
    {
        var receita = ReceitaPrescritorValido();
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(receita);

        var cmd = new DispararAssinaturaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            CallerUsuarioId = _outroMedicoId, // Médico diferente.
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("prescritor"));
    }

    // CA-12: receita já assinada retorna erro.
    [Test]
    public void Handle_ReceitaJaAssinada_LancaBusinessException()
    {
        var receita = ReceitaPrescritorValido();
        receita.IniciarAssinatura();
        receita.ConfirmarAssinatura("s3key");
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(receita);
        _certRepo.Setup(c => c.ObterPorMedicoAsync(_prescritoreId, default)).ReturnsAsync(CertificadoValido());

        var cmd = new DispararAssinaturaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            CallerUsuarioId = _prescritoreId,
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    // CA-05: disparo bem-sucedido → pendente + audit.
    [Test]
    public async Task Handle_PrescritorComCertificado_TransicionaParaPendenteERegistraAudit()
    {
        var receita = ReceitaPrescritorValido();
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(receita);
        _certRepo.Setup(c => c.ObterPorMedicoAsync(_prescritoreId, default)).ReturnsAsync(CertificadoValido());
        _provider.Setup(p => p.DispararAssinaturaAsync(
                It.IsAny<Receita>(), _prescritoreId, It.IsAny<string>(), default))
            .ReturnsAsync(new DisparoAssinaturaResult(Sucesso: true, ModoHomologacao: true));

        var cmd = new DispararAssinaturaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            CallerUsuarioId = _prescritoreId,
        };

        await _sut.Handle(cmd);

        Assert.That(receita.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaPendente));
        _auditRepo.Verify(a => a.SalvarAsync(
            It.Is<AssinaturaAuditLog>(l => l.Acao == "DISPARO_ASSINATURA"),
            default), Times.Once);
    }

    // Sem certificado → erro de negócio.
    [Test]
    public void Handle_SemCertificado_LancaBusinessException()
    {
        var receita = ReceitaPrescritorValido();
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(receita);
        _certRepo.Setup(c => c.ObterPorMedicoAsync(_prescritoreId, default))
            .ReturnsAsync((AssinaturaCertificado?)null);

        var cmd = new DispararAssinaturaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            CallerUsuarioId = _prescritoreId,
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("certificado"));
    }

    // Receita não encontrada → mensagem genérica (multi-tenant).
    [Test]
    public void Handle_ReceitaNaoEncontrada_MensagemGenerica()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId))
            .ReturnsAsync((Receita?)null);

        var cmd = new DispararAssinaturaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            CallerUsuarioId = _prescritoreId,
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Is.EqualTo("Receita não encontrada."));
    }
}
