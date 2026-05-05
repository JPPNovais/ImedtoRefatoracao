using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class AtualizarExameFisicoCommandHandlerTests
{
    private Mock<IExameFisicoRepository> _exameRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private AtualizarExameFisicoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ExameId = 99;
    private readonly Guid _autorId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _exameRepo = new Mock<IExameFisicoRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new AtualizarExameFisicoCommandHandler(_exameRepo.Object, _acessoLog.Object);
    }

    private static ExameFisico ExameNoEstab(long estabId) =>
        ExameFisico.Registrar(
            evolucaoId: 100L, prontuarioId: 200L, pacienteId: 50L,
            estabelecimentoId: estabId, realizadoPorUsuarioId: Guid.NewGuid(),
            realizadoEm: DateTime.UtcNow,
            dadosGeraisJson: null, observacoesGerais: null,
            regioes: new[]
            {
                new ExameFisico.RegiaoInput("CABECA", null, Lateralidade.NaoAplicavel, "Normal", null, 1),
            });

    private AtualizarExameFisicoCommand Cmd() => new()
    {
        ExameFisicoId = ExameId,
        EstabelecimentoId = EstabelecimentoId,
        AutorUsuarioId = _autorId,
        DadosGeraisJson = "{\"peso\":70}",
        ObservacoesGerais = "Sem alteracoes",
        Regioes = new[]
        {
            new RegiaoExameFisicoInput { Codigo = "CABECA", Achados = "Normal" },
        },
    };

    [Test]
    public async Task Handle_DoMesmoTenant_AtualizaEAudita()
    {
        var exame = ExameNoEstab(EstabelecimentoId);
        _exameRepo.Setup(r => r.ObterPorIdOuNulo(ExameId, EstabelecimentoId)).ReturnsAsync(exame);

        await _sut.Handle(Cmd());

        Assert.That(exame.ObservacoesGerais, Is.EqualTo("Sem alteracoes"));
        _exameRepo.Verify(r => r.Salvar(exame), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            200L, _autorId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _exameRepo.Setup(r => r.ObterPorIdOuNulo(ExameId, EstabelecimentoId)).ReturnsAsync((ExameFisico?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Exame físico não encontrado."));
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    [Test]
    public void Handle_JsonInvalido_LancaBusinessException()
    {
        var exame = ExameNoEstab(EstabelecimentoId);
        _exameRepo.Setup(r => r.ObterPorIdOuNulo(ExameId, EstabelecimentoId)).ReturnsAsync(exame);

        var cmd = Cmd();
        cmd.DadosGeraisJson = "{nao eh json valido";

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("JSON"));
    }

    [Test]
    public async Task Handle_RegiaoSumiuDaLista_RemoveDoExame()
    {
        var exame = ExameNoEstab(EstabelecimentoId);
        _exameRepo.Setup(r => r.ObterPorIdOuNulo(ExameId, EstabelecimentoId)).ReturnsAsync(exame);

        var cmd = Cmd();
        cmd.Regioes = Array.Empty<RegiaoExameFisicoInput>(); // remove tudo

        await _sut.Handle(cmd);

        Assert.That(exame.Regioes, Is.Empty, "Regiões fora do payload devem ser removidas.");
    }

    [Test]
    public async Task Handle_NovaRegiao_Adiciona()
    {
        var exame = ExameNoEstab(EstabelecimentoId);
        _exameRepo.Setup(r => r.ObterPorIdOuNulo(ExameId, EstabelecimentoId)).ReturnsAsync(exame);

        var cmd = Cmd();
        cmd.Regioes = new[]
        {
            new RegiaoExameFisicoInput { Codigo = "CABECA", Achados = "Normal" },
            new RegiaoExameFisicoInput { Codigo = "TORAX", Achados = "Sem rales" },
        };

        await _sut.Handle(cmd);

        Assert.That(exame.Regioes.Count, Is.EqualTo(2));
    }

    [Test]
    public void Handle_RegiaoComCodigoVazio_LancaBusinessException()
    {
        var exame = ExameNoEstab(EstabelecimentoId);
        _exameRepo.Setup(r => r.ObterPorIdOuNulo(ExameId, EstabelecimentoId)).ReturnsAsync(exame);

        var cmd = Cmd();
        cmd.Regioes = new[] { new RegiaoExameFisicoInput { Codigo = " " } };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("Código"));
    }
}
