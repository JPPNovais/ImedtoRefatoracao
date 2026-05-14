using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Auth;

/// <summary>
/// Cobre AlterarSenhaAsync — troca de senha autenticada (Correção 2).
///
/// Casos críticos:
///  - Validações de entrada (senha atual vazia, nova curta, igual à atual).
///  - Usuário inexistente / credencial bloqueada → BusinessException.
///  - Senha atual incorreta → BusinessException("Senha atual incorreta.").
///  - Caso feliz: persiste hash novo (diferente do antigo) + revoga refresh tokens.
/// </summary>
[TestFixture]
public class LocalJwtAuthServiceAlterarSenhaTests
{
    private Mock<IAuthCredencialRepository> _credenciaisRepo;
    private Mock<IAuthRefreshTokenRepository> _refreshRepo;
    private Mock<IAuthEmailTokenRepository> _emailTokenRepo;
    private Mock<IPasswordHasher> _hasher;
    private Mock<IJwtTokenIssuer> _issuer;
    private Mock<IEmailService> _emails;
    private LocalJwtAuthService _sut;

    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string SenhaAtualHash = "$2a$12$HashAntigoOK................................";
    private const string SenhaNovaHash  = "$2a$12$HashNovoDiferente........................";

    [SetUp]
    public void SetUp()
    {
        _credenciaisRepo = new Mock<IAuthCredencialRepository>();
        _refreshRepo     = new Mock<IAuthRefreshTokenRepository>();
        _emailTokenRepo  = new Mock<IAuthEmailTokenRepository>();
        _hasher          = new Mock<IPasswordHasher>();
        _issuer          = new Mock<IJwtTokenIssuer>();
        _emails          = new Mock<IEmailService>();

        var emailOptions = Options.Create(new EmailOptions
        {
            From = "noreply@imedto.com",
            AppBaseUrl = "https://app.imedto.com",
        });

        _sut = new LocalJwtAuthService(
            _credenciaisRepo.Object,
            _refreshRepo.Object,
            _emailTokenRepo.Object,
            _hasher.Object,
            _issuer.Object,
            _emails.Object,
            emailOptions,
            new HttpContextAccessor(),
            NullLogger<LocalJwtAuthService>.Instance);
    }

    private static AuthCredencial CredencialAtiva()
    {
        var c = AuthCredencial.Criar(UsuarioId, "joao@imedto.com", SenhaAtualHash);
        c.ConfirmarEmail();
        return c;
    }

    [Test]
    public void AlterarSenha_SenhaAtualVazia_Lanca()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "", novaSenha: "minhasenhaforte"));

        Assert.That(ex!.Message, Is.EqualTo("Informe sua senha atual."));
        // Não deve nem buscar a credencial.
        _credenciaisRepo.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public void AlterarSenha_SenhaAtualNull_Lanca()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: null, novaSenha: "minhasenhaforte"));

        Assert.That(ex!.Message, Is.EqualTo("Informe sua senha atual."));
    }

    [Test]
    public void AlterarSenha_NovaSenhaIgualAtual_Lanca()
    {
        // Bloqueio explícito ANTES do hasher — não exige hit no banco.
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "mesmasenha123", novaSenha: "mesmasenha123"));

        Assert.That(ex!.Message, Is.EqualTo("A nova senha precisa ser diferente da atual."));
        _credenciaisRepo.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public void AlterarSenha_NovaSenhaCurta_LancaErroDeForca()
    {
        // 7 caracteres < SenhaMinima (8).
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "atual123", novaSenha: "curta1!"));

        Assert.That(ex!.Message, Does.Contain("mínimo 8 caracteres"));
    }

    [Test]
    public void AlterarSenha_NovaSenhaVazia_LancaErroDeForca()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "atualSenha123", novaSenha: ""));

        Assert.That(ex!.Message, Does.Contain("mínimo 8 caracteres"));
    }

    [Test]
    public void AlterarSenha_UsuarioInexistente_Lanca()
    {
        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId))
                        .ReturnsAsync((AuthCredencial)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "atualSenha123", novaSenha: "novaSenha456"));

        Assert.That(ex!.Message, Is.EqualTo("Conta não encontrada."));
    }

    [Test]
    public void AlterarSenha_CredencialBloqueada_Lanca()
    {
        var credencial = CredencialAtiva();
        credencial.Bloquear("teste");
        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(credencial);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "atualSenha123", novaSenha: "novaSenha456"));

        Assert.That(ex!.Message, Is.EqualTo("Conta bloqueada. Contate o suporte."));
        // Não deve nem verificar senha atual.
        _hasher.Verify(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void AlterarSenha_SenhaAtualIncorreta_Lanca()
    {
        var credencial = CredencialAtiva();
        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(credencial);
        _hasher.Setup(h => h.Verificar("senhaErrada123", SenhaAtualHash)).Returns(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "senhaErrada123", novaSenha: "novaSenha456"));

        Assert.That(ex!.Message, Is.EqualTo("Senha atual incorreta."));
        // Não deve persistir nem revogar sessões.
        _credenciaisRepo.Verify(r => r.Atualizar(It.IsAny<AuthCredencial>()), Times.Never);
        _refreshRepo.Verify(r => r.RevogarTodosDoUsuarioAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task AlterarSenha_Sucesso_AtualizaHashERevogaTodosOsRefreshTokens()
    {
        var credencial = CredencialAtiva();
        var hashAntes = credencial.SenhaHash;

        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(credencial);
        _hasher.Setup(h => h.Verificar("atualSenha123", SenhaAtualHash)).Returns(true);
        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        await _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "atualSenha123", novaSenha: "novaSenha456");

        // Hash de fato mudou (e veio do hasher, não foi reaproveitado).
        Assert.That(credencial.SenhaHash, Is.EqualTo(SenhaNovaHash));
        Assert.That(credencial.SenhaHash, Is.Not.EqualTo(hashAntes));

        // Persistência + revogação total das sessões do usuário (boa prática pós-troca).
        _credenciaisRepo.Verify(r => r.Atualizar(credencial), Times.Once);
        _refreshRepo.Verify(r => r.RevogarTodosDoUsuarioAsync(UsuarioId), Times.Once);

        // Não foi feita nenhuma emissão de e-mail nem token (não é fluxo de e-mail).
        _emailTokenRepo.Verify(r => r.AdicionarAsync(It.IsAny<AuthEmailToken>()), Times.Never);
        _emails.Verify(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task AlterarSenha_Sucesso_HasherChamadoParaGerarHashNovo()
    {
        // Garante que o NOVO hash veio do hasher (Hash chamado com a senha nova),
        // e não foi reaproveitado o hash antigo.
        var credencial = CredencialAtiva();
        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(credencial);
        _hasher.Setup(h => h.Verificar("atualSenha123", SenhaAtualHash)).Returns(true);
        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        await _sut.AlterarSenhaAsync(UsuarioId, senhaAtual: "atualSenha123", novaSenha: "novaSenha456");

        _hasher.Verify(h => h.Hash("novaSenha456"), Times.Once);
        // Não pode ter usado a senha atual pra gerar o hash novo.
        _hasher.Verify(h => h.Hash("atualSenha123"), Times.Never);
    }
}
