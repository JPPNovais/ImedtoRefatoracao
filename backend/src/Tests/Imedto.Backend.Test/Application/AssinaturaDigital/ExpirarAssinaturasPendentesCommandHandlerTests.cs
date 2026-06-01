using System.Collections.Generic;
using Imedto.Backend.Application.AssinaturaDigital.Commands;
using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.AssinaturaDigital;

[TestFixture]
public class ExpirarAssinaturasPendentesCommandHandlerTests
{
    private Mock<IReceitaRepository> _receitaRepo = null!;
    private Mock<IAssinaturaAuditLogRepository> _auditRepo = null!;
    private ExpirarAssinaturasPendentesCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _receitaRepo = new Mock<IReceitaRepository>();
        _auditRepo = new Mock<IAssinaturaAuditLogRepository>();
        _sut = new ExpirarAssinaturasPendentesCommandHandler(
            _receitaRepo.Object,
            _auditRepo.Object,
            NullLogger<ExpirarAssinaturasPendentesCommandHandler>.Instance);
    }

    private static Receita ReceitaPendente(long id, long estabelecimentoId = 1)
    {
        var itens = new List<(string, string, string?, ViaAdministracao?, string?)> { ("Dipirona", "1 cp", null, null, null) };
        var r = Receita.Emitir(1, 2, Guid.NewGuid(), estabelecimentoId, TipoReceita.Comum, null, null, itens);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(r, id);
        r.IniciarAssinatura();
        return r;
    }

    // CA-10: receitas pendentes com mais de 30 min → expiradas.
    [Test]
    public async Task Handle_ReceitasPendentesVencidas_ExpiraEToda()
    {
        var r1 = ReceitaPendente(1);
        var r2 = ReceitaPendente(2);

        _receitaRepo.Setup(r => r.ListarPendentesParaExpirarAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Receita> { r1, r2 });

        await _sut.Handle(new ExpirarAssinaturasPendentesCommand { LimiteMinutos = 30 });

        Assert.That(r1.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaExpirada));
        Assert.That(r2.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaExpirada));
        _auditRepo.Verify(a => a.SalvarAsync(
            It.Is<AssinaturaAuditLog>(l => l.Acao == "EXPIRAR_PENDENTE"),
            default), Times.Exactly(2));
    }

    // CA-10: sem receitas elegíveis → nenhuma mutação.
    [Test]
    public async Task Handle_SemReceitasElegiveis_NenhumaMutacao()
    {
        _receitaRepo.Setup(r => r.ListarPendentesParaExpirarAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Receita>());

        await _sut.Handle(new ExpirarAssinaturasPendentesCommand { LimiteMinutos = 30 });

        _receitaRepo.Verify(r => r.Salvar(It.IsAny<Receita>()), Times.Never);
        _auditRepo.Verify(a => a.SalvarAsync(It.IsAny<AssinaturaAuditLog>(), default), Times.Never);
    }

    // Falha em uma receita não interrompe o lote.
    [Test]
    public async Task Handle_FalhaEmUmaReceita_ContinuaRestante()
    {
        var r1 = ReceitaPendente(1);
        var r2 = ReceitaPendente(2);

        _receitaRepo.Setup(r => r.ListarPendentesParaExpirarAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Receita> { r1, r2 });

        // Primeira receita: Salvar lança exceção.
        _receitaRepo.Setup(r => r.Salvar(r1)).ThrowsAsync(new Exception("DB error"));

        await _sut.Handle(new ExpirarAssinaturasPendentesCommand { LimiteMinutos = 30 });

        // Segunda receita deve ter sido processada mesmo com falha na primeira.
        Assert.That(r2.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.AssinaturaExpirada));
    }
}
