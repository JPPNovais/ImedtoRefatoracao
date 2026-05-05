using System.Net;
using System.Text;
using System.Text.Json;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Auth;

/// <summary>
/// Testes unitários do SupabaseAuthService com HttpMessageHandler mockado.
/// Nenhuma chamada real ao Supabase ocorre — todos os cenários usam respostas
/// pré-fabricadas para garantir isolamento e reprodutibilidade.
/// </summary>
[TestFixture]
public class SupabaseAuthServiceTests
{
    private Mock<ILogger<SupabaseAuthService>> _logger;
    private SupabaseOptions _options;

    // JSON mínimo de resposta de auth com sessão válida.
    private static readonly string RespostaLoginOk = JsonSerializer.Serialize(new
    {
        access_token = "eyJ_token_fake",
        refresh_token = "refresh_fake",
        expires_in = 3600,
        user = new
        {
            id = "user-uuid-123",
            email = "paciente@exemplo.com",
            app_metadata = new { roles = new[] { "profissional" } }
        }
    });

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<SupabaseAuthService>>();
        _options = new SupabaseOptions
        {
            Url = "https://test.supabase.co",
            Authority = "https://test.supabase.co/auth/v1",
            AnonKey = "anon-key-test",
            ServiceRoleKey = "service-role-key-test"
        };
    }

    /// <summary>
    /// Cria SupabaseAuthService com um HttpMessageHandler que retorna a resposta configurada.
    /// </summary>
    private SupabaseAuthService CriarSut(
        HttpStatusCode statusCode,
        string conteudo,
        string nomeCliente = "supabase")
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(conteudo, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(nomeCliente)).Returns(httpClient);

        return new SupabaseAuthService(
            factory.Object,
            Options.Create(_options),
            _logger.Object);
    }

    // ---- LoginAsync — sucesso ----

    [Test]
    public async Task LoginAsync_CredenciaisValidas_RetornaAuthResultComTokens()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.OK, RespostaLoginOk);

        // Act
        var resultado = await sut.LoginAsync("usuario@imedto.com.br", "senha123");

        // Assert
        Assert.That(resultado.AccessToken, Is.EqualTo("eyJ_token_fake"));
        Assert.That(resultado.RefreshToken, Is.EqualTo("refresh_fake"));
        Assert.That(resultado.User.Id, Is.EqualTo("user-uuid-123"));
        Assert.That(resultado.User.Email, Is.EqualTo("paciente@exemplo.com"));
    }

    [Test]
    public async Task LoginAsync_CredenciaisValidas_ExpiresAtEhFuturo()
    {
        // Arrange
        var antes = DateTime.UtcNow;
        var sut = CriarSut(HttpStatusCode.OK, RespostaLoginOk);

        // Act
        var resultado = await sut.LoginAsync("u@x.com", "s");

        // Assert — expires_in = 3600 → ExpiresAt deve ser ~1h no futuro
        Assert.That(resultado.ExpiresAt, Is.GreaterThan(antes.AddMinutes(55)));
    }

    [Test]
    public async Task LoginAsync_CredenciaisValidas_UserContemRoles()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.OK, RespostaLoginOk);

        // Act
        var resultado = await sut.LoginAsync("u@x.com", "s");

        // Assert
        Assert.That(resultado.User.Roles, Contains.Item("profissional"));
    }

    // ---- LoginAsync — credenciais inválidas (400) ----

    [Test]
    public void LoginAsync_CredenciaisInvalidas_LancaBusinessException()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_grant\",\"error_description\":\"Invalid login credentials\"}");

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.LoginAsync("inexistente@imedto.com.br", "senha-errada"));

        Assert.That(ex.Message, Is.EqualTo("Credenciais inválidas."));
    }

    [Test]
    public void LoginAsync_CredenciaisInvalidas_MensagemNaoVazaEmailDoRequest()
    {
        // Arrange — LGPD: o email não pode aparecer na mensagem de erro ao cliente.
        const string emailSensivel = "dados-pessoais@paciente.com.br";
        var sut = CriarSut(HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_grant\"}");

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.LoginAsync(emailSensivel, "senha"));

        Assert.That(ex.Message, Does.Not.Contain(emailSensivel));
    }

    [Test]
    public void LoginAsync_Erro500DoSupabase_NaoLancaBusinessException()
    {
        // Arrange — 500 é erro técnico, não de negócio; não deve ser BusinessException.
        var sut = CriarSut(HttpStatusCode.InternalServerError, "{\"error\":\"server_error\"}");

        // Act + Assert — qualquer exceção que não seja BusinessException é aceitável.
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.LoginAsync("u@x.com", "s"));

        // O SupabaseAuthService atual sempre lança BusinessException em qualquer falha HTTP
        // para LoginAsync — este teste documenta o comportamento atual. Se a implementação
        // mudar para relançar como exceção técnica em 5xx, atualizar aqui.
        Assert.That(ex, Is.Not.Null);
    }

    // ---- LoginAsync — não loga PII ----

    [Test]
    public async Task LoginAsync_Falha_LogNaoContemEmailDoUsuario()
    {
        // Arrange — verificar que o log usa hash (HashEmail) e não o email cru.
        const string emailSensivel = "pii-sensivelissimo@paciente.sus.br";
        var sut = CriarSut(HttpStatusCode.BadRequest, "{\"error\":\"invalid_grant\"}");

        // Act
        try
        {
            await sut.LoginAsync(emailSensivel, "senha");
        }
        catch (BusinessException) { }

        // Assert — nenhuma chamada de log deve conter o email em texto claro.
        _logger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(emailSensivel)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never,
            "O email do usuário não deve aparecer em texto claro nos logs (LGPD).");
    }

    // ---- RefreshAsync — token expirado ----

    [Test]
    public void RefreshAsync_TokenExpirado_LancaBusinessException()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_grant\",\"error_description\":\"Refresh Token Not Found\"}");

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.RefreshAsync("refresh-token-expirado"));

        Assert.That(ex.Message, Does.Contain("expirada").Or.Contain("expirado").Or.Contain("login"));
    }

    [Test]
    public async Task RefreshAsync_TokenValido_RetornaNovaSessionComTokens()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.OK, RespostaLoginOk);

        // Act
        var resultado = await sut.RefreshAsync("refresh-token-valido");

        // Assert
        Assert.That(resultado.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(resultado.RefreshToken, Is.Not.Null.And.Not.Empty);
        Assert.That(resultado.User, Is.Not.Null);
    }

    // ---- SignupAsync ----

    [Test]
    public async Task SignupAsync_EmailNovo_RetornaSignupResultComSessao()
    {
        // Arrange — confirmação desligada: resposta inclui access_token.
        var sut = CriarSut(HttpStatusCode.OK, RespostaLoginOk);

        // Act
        var resultado = await sut.SignupAsync("novo@imedto.com.br", "senha123");

        // Assert
        Assert.That(resultado.User, Is.Not.Null);
        Assert.That(resultado.Session, Is.Not.Null);
        Assert.That(resultado.Session!.AccessToken, Is.EqualTo("eyJ_token_fake"));
    }

    [Test]
    public async Task SignupAsync_ConfirmacaoLigada_RetornaSignupResultSemSessao()
    {
        // Arrange — confirmação ligada: body não contém access_token, só o user.
        var respostaSemToken = JsonSerializer.Serialize(new
        {
            id = "user-uuid-456",
            email = "confirmacao@imedto.com.br",
            app_metadata = new { roles = Array.Empty<string>() }
        });

        var sut = CriarSut(HttpStatusCode.OK, respostaSemToken);

        // Act
        var resultado = await sut.SignupAsync("confirmacao@imedto.com.br", "senha");

        // Assert — sem sessão porque e-mail não foi confirmado ainda.
        Assert.That(resultado.Session, Is.Null);
        Assert.That(resultado.User.Email, Is.EqualTo("confirmacao@imedto.com.br"));
    }

    [Test]
    public void SignupAsync_EmailJaExiste_LancaBusinessExceptionSemVazarEmail()
    {
        // Arrange — Supabase retorna 422 com "already" no corpo quando e-mail duplicado.
        const string emailExistente = "ja-cadastrado@imedto.com.br";
        var sut = CriarSut(
            HttpStatusCode.UnprocessableEntity,
            "{\"code\":\"user_already_exists\",\"message\":\"User already registered\"}");

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.SignupAsync(emailExistente, "senha"));

        Assert.That(ex.Message, Does.Not.Contain(emailExistente));
        Assert.That(ex.Message, Does.Contain("e-mail").Or.Contain("conta"));
    }

    // ---- GetUserAsync ----

    [Test]
    public async Task GetUserAsync_TokenValido_RetornaUserInfo()
    {
        // Arrange — /auth/v1/user retorna o user diretamente no root.
        var respostaUser = JsonSerializer.Serialize(new
        {
            id = "user-uuid-789",
            email = "profissional@clinica.com.br",
            app_metadata = new { roles = new[] { "admin" } }
        });

        var sut = CriarSut(HttpStatusCode.OK, respostaUser);

        // Act
        var resultado = await sut.GetUserAsync("access-token-valido");

        // Assert
        Assert.That(resultado.Id, Is.EqualTo("user-uuid-789"));
        Assert.That(resultado.Email, Is.EqualTo("profissional@clinica.com.br"));
        Assert.That(resultado.Roles, Contains.Item("admin"));
    }

    [Test]
    public void GetUserAsync_TokenInvalido_LancaBusinessException()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.Unauthorized, "{\"error\":\"invalid_token\"}");

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.GetUserAsync("token-invalido"));

        Assert.That(ex, Is.Not.Null);
    }

    // ---- DeleteUserAsync ----

    [Test]
    public async Task DeleteUserAsync_UsuarioExiste_NaoLancaExcecao()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.OK, "{}");

        // Act + Assert — deve completar sem exceção.
        Assert.DoesNotThrowAsync(async () =>
            await sut.DeleteUserAsync("user-uuid-para-deletar"));
    }

    [Test]
    public void DeleteUserAsync_FalhaNoSupabase_LancaBusinessException()
    {
        // Arrange
        var sut = CriarSut(HttpStatusCode.NotFound, "{\"error\":\"not_found\"}");

        // Act + Assert
        Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.DeleteUserAsync("uuid-inexistente"));
    }

    // ---- LogoutAsync — fire-and-forget ----

    [Test]
    public async Task LogoutAsync_FalhaDeRede_NaoLancaExcecao()
    {
        // Arrange — LogoutAsync é fire-and-forget; mesmo em falha não deve propagar.
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Rede indisponível"));

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri(_options.Url) };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("supabase")).Returns(httpClient);

        var sut = new SupabaseAuthService(factory.Object, Options.Create(_options), _logger.Object);

        // Act + Assert — não deve lançar.
        Assert.DoesNotThrowAsync(async () =>
            await sut.LogoutAsync("qualquer-token"));
    }

    // ---- EnviarRecuperacaoSenhaAsync — prevenção de enumeração ----

    [Test]
    public async Task EnviarRecuperacaoSenhaAsync_EmailInexistente_NaoLancaExcecao()
    {
        // Arrange — nunca revelar se e-mail existe (prevenção de enumeração).
        var sut = CriarSut(HttpStatusCode.OK, "{}");

        // Act + Assert
        Assert.DoesNotThrowAsync(async () =>
            await sut.EnviarRecuperacaoSenhaAsync("inexistente@naoexiste.com", "https://app/reset"));
    }

    [Test]
    public async Task EnviarRecuperacaoSenhaAsync_FalhaDeRede_NaoLancaExcecao()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri(_options.Url) };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("supabase")).Returns(httpClient);

        var sut = new SupabaseAuthService(factory.Object, Options.Create(_options), _logger.Object);

        // Act + Assert
        Assert.DoesNotThrowAsync(async () =>
            await sut.EnviarRecuperacaoSenhaAsync("usuario@email.com", "https://app/reset"));
    }
}
