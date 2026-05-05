using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Receitas;

[TestFixture]
public class CancelarReceitaCommandHandlerTests
{
    private Mock<IReceitaRepository> _receitaRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private CancelarReceitaCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ReceitaId = 99;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _receitaRepo = new Mock<IReceitaRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new CancelarReceitaCommandHandler(_receitaRepo.Object, _acessoLog.Object);
    }

    private static Receita ReceitaEmitida(long estabId, Guid prof) =>
        Receita.Emitir(
            prontuarioId: 200L,
            pacienteId: 100L,
            profissionalUsuarioId: prof,
            estabelecimentoId: estabId,
            tipo: TipoReceita.Comum,
            observacoes: null,
            validadeAte: null,
            itens: new[] { ("Dipirona 500mg", "1 cp 6/6h", (string)null, (ViaAdministracao?)null, (string)null) });

    [Test]
    public async Task Handle_DoMesmoTenant_CancelaEAudita()
    {
        var receita = ReceitaEmitida(EstabelecimentoId, _solicitanteId);
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(receita);

        await _sut.Handle(new CancelarReceitaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            Motivo = "Erro de prescricao",
        });

        Assert.That(receita.Status, Is.EqualTo(StatusReceita.Cancelada));
        _receitaRepo.Verify(r => r.Salvar(receita), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            200L, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaBusinessExceptionENaoAudita()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId))
                    .ReturnsAsync((Receita?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarReceitaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            Motivo = "tentativa cross-tenant",
        }));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    [Test]
    public void Handle_Inexistente_LancaBusinessException()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync((Receita)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarReceitaCommand
        {
            ReceitaId = ReceitaId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId,
            Motivo = "X",
        }));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }
}
