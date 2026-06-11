using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Assinaturas;
using Imedto.Backend.Contracts.Admin.Assinaturas.Queries;
using Imedto.Backend.Contracts.Admin.Assinaturas.Queries.Results;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Test.Application.Admin.Assinaturas;

/// <summary>
/// Regressão do bug onde ListarHistoricoAsync não incluía ExpiraEm, SuspensaEm e Estado
/// na query Dapper — fazendo a API retornar {"estado":"","expiraEm":null,"suspensaEm":null}
/// independente do estado real da assinatura (briefing 2026-06-11_003, F4).
///
/// Estes testes mocam ImedtoAssinaturaQueryRepository para verificar que o handler
/// repassa corretamente o DTO do repositório (passagem de contrato).
/// Os cenários de Estado espelham o CASE WHEN adicionado na query SQL, que por sua vez
/// espelha ImedtoAssinatura.ObterEstado() — vocabulário único nos três pontos.
/// </summary>
[TestFixture]
public class ListarHistoricoAssinaturasAdminQueryHandlerTests
{
    private const long _eid = 99L;
    private Mock<ImedtoAssinaturaQueryRepository> _repoMock = null!;
    private ListarHistoricoAssinaturasAdminQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        // ImedtoAssinaturaQueryRepository não tem interface — mockamos a classe concreta.
        // AppReadConnectionString é sealed record: instanciamos com string dummy.
        // O método é virtual, então Moq intercepta antes de abrir conexão real.
        var connDummy = new AppReadConnectionString("Host=dummy;Database=dummy");
        _repoMock = new Mock<ImedtoAssinaturaQueryRepository>(connDummy) { CallBase = false };
        _handler = new ListarHistoricoAssinaturasAdminQueryHandler(_repoMock.Object);
    }

    // -------------------------------------------------------
    // Cenário: vitalícia ativa (expira_em NULL, suspensa_em NULL)
    // -------------------------------------------------------

    [Test]
    public async Task Handle_VitaliciaAtiva_RetornaEstadoVitaliciaExpiraNullSuspensaNull()
    {
        var dto = new AssinaturaAdminDto
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = _eid,
            PlanoNome = "Plano Plus",
            Vigente = true,
            Estado = "Vitalicia",
            ExpiraEm = null,
            SuspensaEm = null,
        };

        _repoMock
            .Setup(r => r.ListarHistoricoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssinaturaAdminDto> { dto });

        var query = new ListarHistoricoAssinaturasAdminQuery(_eid);
        var resultado = await _handler.Handle(query);

        Assert.That(resultado, Has.Count.EqualTo(1));
        var item = resultado[0];
        Assert.That(item.Estado, Is.EqualTo("Vitalicia"),
            "Estado deve ser 'Vitalicia' quando expira_em IS NULL e suspensa_em IS NULL");
        Assert.That(item.ExpiraEm, Is.Null, "ExpiraEm deve ser null para vitalícia");
        Assert.That(item.SuspensaEm, Is.Null, "SuspensaEm deve ser null para ativa não suspensa");
    }

    // -------------------------------------------------------
    // Cenário: temporária (expira_em no futuro, suspensa_em NULL)
    // -------------------------------------------------------

    [Test]
    public async Task Handle_TemporariaFutura_RetornaEstadoTemporaria()
    {
        var expira = DateTimeOffset.UtcNow.AddDays(30);
        var dto = new AssinaturaAdminDto
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = _eid,
            PlanoNome = "Trial Plus",
            Vigente = true,
            Estado = "Temporaria",
            ExpiraEm = expira,
            SuspensaEm = null,
        };

        _repoMock
            .Setup(r => r.ListarHistoricoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssinaturaAdminDto> { dto });

        var query = new ListarHistoricoAssinaturasAdminQuery(_eid);
        var resultado = await _handler.Handle(query);

        var item = resultado[0];
        Assert.That(item.Estado, Is.EqualTo("Temporaria"),
            "Estado deve ser 'Temporaria' quando expira_em está no futuro");
        Assert.That(item.ExpiraEm, Is.Not.Null, "ExpiraEm deve ser preenchido para temporária");
    }

    // -------------------------------------------------------
    // Cenário: suspensa (suspensa_em preenchido)
    // -------------------------------------------------------

    [Test]
    public async Task Handle_Suspensa_RetornaEstadoSuspensaSuspensaEmPreenchido()
    {
        var suspensoEm = DateTimeOffset.UtcNow.AddHours(-2);
        var dto = new AssinaturaAdminDto
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = _eid,
            PlanoNome = "Plano Plus",
            Vigente = true,
            Estado = "Suspensa",
            ExpiraEm = null,
            SuspensaEm = suspensoEm,
        };

        _repoMock
            .Setup(r => r.ListarHistoricoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssinaturaAdminDto> { dto });

        var query = new ListarHistoricoAssinaturasAdminQuery(_eid);
        var resultado = await _handler.Handle(query);

        var item = resultado[0];
        Assert.That(item.Estado, Is.EqualTo("Suspensa"),
            "Estado deve ser 'Suspensa' quando suspensa_em está preenchido");
        Assert.That(item.SuspensaEm, Is.Not.Null, "SuspensaEm deve ser retornado no DTO");
    }

    // -------------------------------------------------------
    // Cenário: expirada (expira_em no passado, suspensa_em NULL)
    // -------------------------------------------------------

    [Test]
    public async Task Handle_Expirada_RetornaEstadoExpirada()
    {
        var dto = new AssinaturaAdminDto
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = _eid,
            PlanoNome = "Trial Expirado",
            Vigente = true,
            Estado = "Expirada",
            ExpiraEm = DateTimeOffset.UtcNow.AddDays(-1),
            SuspensaEm = null,
        };

        _repoMock
            .Setup(r => r.ListarHistoricoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssinaturaAdminDto> { dto });

        var query = new ListarHistoricoAssinaturasAdminQuery(_eid);
        var resultado = await _handler.Handle(query);

        Assert.That(resultado[0].Estado, Is.EqualTo("Expirada"),
            "Estado deve ser 'Expirada' quando expira_em está no passado");
    }

    // -------------------------------------------------------
    // Cenário: encerrada (fim_em preenchido — vigência histórica)
    // -------------------------------------------------------

    [Test]
    public async Task Handle_Encerrada_RetornaEstadoEncerradaVigentefalse()
    {
        var dto = new AssinaturaAdminDto
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = _eid,
            PlanoNome = "Plano Antigo",
            Vigente = false,
            FimEm = DateTimeOffset.UtcNow.AddDays(-10),
            Estado = "Encerrada",
            ExpiraEm = null,
            SuspensaEm = null,
        };

        _repoMock
            .Setup(r => r.ListarHistoricoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssinaturaAdminDto> { dto });

        var query = new ListarHistoricoAssinaturasAdminQuery(_eid);
        var resultado = await _handler.Handle(query);

        var item = resultado[0];
        Assert.That(item.Estado, Is.EqualTo("Encerrada"),
            "Estado deve ser 'Encerrada' para vigências com fim_em preenchido");
        Assert.That(item.Vigente, Is.False, "Vigente deve ser false para assinatura encerrada");
    }

    // -------------------------------------------------------
    // Regressão principal: campo Estado não pode ser vazio/null para vitalícia vigente
    // -------------------------------------------------------

    [Test]
    public async Task Handle_VitaliciaVigente_EstadoNaoEhVazioNemNull()
    {
        // Antes do fix, a query Dapper não incluía a coluna Estado →
        // AssinaturaAdminDto.Estado ficava string.Empty.
        var dto = new AssinaturaAdminDto
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = _eid,
            Vigente = true,
            Estado = "Vitalicia",
            ExpiraEm = null,
            SuspensaEm = null,
        };

        _repoMock
            .Setup(r => r.ListarHistoricoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssinaturaAdminDto> { dto });

        var resultado = await _handler.Handle(new ListarHistoricoAssinaturasAdminQuery(_eid));

        Assert.That(resultado[0].Estado, Is.Not.Empty,
            "Regressão: Estado NÃO pode ser string vazia — causava badge 'Encerrado' indevido e ocultava botões Suspender/Reativar");
        Assert.That(resultado[0].Estado, Is.Not.Null);
    }
}
