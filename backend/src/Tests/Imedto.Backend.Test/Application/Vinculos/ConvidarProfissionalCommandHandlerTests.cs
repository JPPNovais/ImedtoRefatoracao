using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class ConvidarProfissionalCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _estabelecimentoRepo;
    private Mock<IModeloPermissaoRepository> _modeloRepo;
    private Mock<IUsuarioRepository> _usuarioRepo;
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IEventBus> _eventBus;
    private Mock<IAssinaturaService> _assinaturaService;
    private Mock<CatalogoQueryRepository> _catalogoRepo;
    private ConvidarProfissionalCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _profissionalId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long ModeloId = 10;

    [SetUp]
    public void SetUp()
    {
        _estabelecimentoRepo = new Mock<IEstabelecimentoRepository>();
        _modeloRepo = new Mock<IModeloPermissaoRepository>();
        _usuarioRepo = new Mock<IUsuarioRepository>();
        _vinculoRepo = new Mock<IVinculoRepository>();
        _eventBus = new Mock<IEventBus>();
        _assinaturaService = new Mock<IAssinaturaService>();
        _catalogoRepo = new Mock<CatalogoQueryRepository>(
            new Imedto.Backend.Infrastructure.AppReadConnectionString("Host=localhost;Database=fake"));

        // Padrão: plano sem limite atingido (não bloqueia).
        _assinaturaService
            .Setup(s => s.LimiteAtingidoAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Catálogo padrão: aceita qualquer profissão/especialidade (testes que não mexem com isso passam).
        _catalogoRepo.Setup(r => r.ExisteProfissaoAtiva(It.IsAny<long>())).ReturnsAsync(true);
        _catalogoRepo.Setup(r => r.ExisteEspecialidadeAtivaPorNome(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _sut = new ConvidarProfissionalCommandHandler(
            _estabelecimentoRepo.Object,
            _modeloRepo.Object,
            _usuarioRepo.Object,
            _vinculoRepo.Object,
            _eventBus.Object,
            _assinaturaService.Object,
            _catalogoRepo.Object);
    }

    private Estabelecimento CriarEstabelecimento() =>
        Estabelecimento.Criar(_donoId, "Clínica Teste", null, null, null, null);

    private ModeloPermissaoEstabelecimento CriarModelo()
    {
        // Usa reflection para criar instância com EhPadrao = true — construtor é protected
        var modelo = (ModeloPermissaoEstabelecimento)
            Activator.CreateInstance(typeof(ModeloPermissaoEstabelecimento), nonPublic: true)!;
        typeof(ModeloPermissaoEstabelecimento)
            .GetProperty(nameof(ModeloPermissaoEstabelecimento.Id),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(modelo, ModeloId);
        return modelo;
    }

    private void ConfigurarMocksBase()
    {
        _estabelecimentoRepo
            .Setup(r => r.ObterPorId(EstabelecimentoId))
            .ReturnsAsync(CriarEstabelecimento());

        _modeloRepo
            .Setup(r => r.ObterPadraoDoEstabelecimento(EstabelecimentoId))
            .ReturnsAsync(CriarModelo());

        _usuarioRepo
            .Setup(r => r.ObterPorIdOuNulo(_profissionalId))
            .ReturnsAsync((Usuario)null);

        _usuarioRepo
            .Setup(r => r.Salvar(It.IsAny<Usuario>()))
            .Returns(Task.CompletedTask);

        // Simula o comportamento do repositório EF: popula Id ao salvar entidade nova
        _vinculoRepo
            .Setup(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()))
            .Callback<VinculoProfissionalEstabelecimento>(v =>
            {
                if (v.Id == 0)
                    typeof(VinculoProfissionalEstabelecimento)
                        .GetProperty(nameof(VinculoProfissionalEstabelecimento.Id),
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
                        .SetValue(v, 1L);
            })
            .Returns(Task.CompletedTask);

        _eventBus
            .Setup(b => b.Publish(It.IsAny<IDomainEvent>()))
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_ProfissionalSemVinculoPrevio_CriaNovo_EPublicaEvento()
    {
        // Arrange
        ConfigurarMocksBase();
        _vinculoRepo
            .Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
            .ReturnsAsync((VinculoProfissionalEstabelecimento)null);

        var cmd = new ConvidarProfissionalCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            ConvidadoPorUsuarioId = _donoId,
            ProfissionalUsuarioId = _profissionalId,
            ProfissionalEmail = "prof@clinica.com",
            ModeloPermissaoId = null
        };

        // Act
        await _sut.Handle(cmd);

        // Assert
        _vinculoRepo.Verify(r => r.Salvar(It.Is<VinculoProfissionalEstabelecimento>(v =>
            v.ProfissionalUsuarioId == _profissionalId &&
            v.EstabelecimentoId == EstabelecimentoId &&
            v.Status == VinculoStatus.Convidado)), Times.Once);

        _eventBus.Verify(b => b.Publish(It.IsAny<IDomainEvent>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task Handle_ProfissionalComVinculoInativo_ReativaRegistroExistente_EPublicaEvento()
    {
        // Arrange
        ConfigurarMocksBase();

        var vinculoInativo = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1, _donoId);
        vinculoInativo.Aceitar();
        vinculoInativo.Inativar();

        _vinculoRepo
            .Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
            .ReturnsAsync(vinculoInativo);

        var cmd = new ConvidarProfissionalCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            ConvidadoPorUsuarioId = _donoId,
            ProfissionalUsuarioId = _profissionalId,
            ProfissionalEmail = "prof@clinica.com",
            ModeloPermissaoId = null
        };

        // Act
        await _sut.Handle(cmd);

        // Assert — salva o vínculo reativado (não cria novo)
        _vinculoRepo.Verify(r => r.Salvar(It.Is<VinculoProfissionalEstabelecimento>(v =>
            v.Status == VinculoStatus.Convidado &&
            v.AceitoEm == null &&
            v.InativadoEm == null)), Times.Once);

        _eventBus.Verify(b => b.Publish(It.IsAny<IDomainEvent>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task Handle_ProfissionalComVinculoAtivo_LancaBusinessException()
    {
        // Arrange
        ConfigurarMocksBase();

        var vinculoAtivo = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1, _donoId);
        vinculoAtivo.Aceitar();

        _vinculoRepo
            .Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
            .ReturnsAsync(vinculoAtivo);

        var cmd = new ConvidarProfissionalCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            ConvidadoPorUsuarioId = _donoId,
            ProfissionalUsuarioId = _profissionalId,
            ProfissionalEmail = "prof@clinica.com"
        };

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));

        Assert.That(ex.Message, Does.Contain("ativo").IgnoreCase.Or.Contain("pendente").IgnoreCase);
    }

    [Test]
    public async Task Handle_ProfissionalComVinculoConvidado_LancaBusinessException()
    {
        // Arrange
        ConfigurarMocksBase();

        var vinculoPendente = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1, _donoId);
        // Status = Convidado (padrão após Convidar)

        _vinculoRepo
            .Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
            .ReturnsAsync(vinculoPendente);

        var cmd = new ConvidarProfissionalCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            ConvidadoPorUsuarioId = _donoId,
            ProfissionalUsuarioId = _profissionalId,
            ProfissionalEmail = "prof@clinica.com"
        };

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));

        Assert.That(ex.Message, Does.Contain("pendente").IgnoreCase.Or.Contain("ativo").IgnoreCase);
    }
}
