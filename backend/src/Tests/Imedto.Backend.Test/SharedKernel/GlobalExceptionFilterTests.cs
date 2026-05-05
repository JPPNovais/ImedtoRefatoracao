using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.SharedKernel;

[TestFixture]
public class GlobalExceptionFilterTests
{
    private Mock<ILogger<GlobalExceptionFilter>> _logger;
    private Mock<IHostEnvironment> _env;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<GlobalExceptionFilter>>();
        _env = new Mock<IHostEnvironment>();

        // Padrão: ambiente de produção — não vaza detalhes de exceção.
        _env.Setup(e => e.EnvironmentName).Returns(Environments.Production);
    }

    private GlobalExceptionFilter CriarSut() =>
        new GlobalExceptionFilter(_logger.Object, _env.Object);

    private static ExceptionContext CriarContexto(Exception ex, string path = "/api/teste")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = ex
        };
    }

    // ---- BusinessException ----

    [Test]
    public void OnException_BusinessException_RetornaHttp422()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarContexto(new BusinessException("Preço deve ser maior que zero."));

        // Act
        sut.OnException(context);

        // Assert
        var resultado = context.Result as UnprocessableEntityObjectResult;
        Assert.That(resultado, Is.Not.Null);
        Assert.That(resultado.StatusCode, Is.EqualTo(422));
    }

    [Test]
    public void OnException_BusinessException_PayloadContemMensagemCorreta()
    {
        // Arrange
        const string mensagemEsperada = "Preço deve ser maior que zero.";
        var sut = CriarSut();
        var context = CriarContexto(new BusinessException(mensagemEsperada));

        // Act
        sut.OnException(context);

        // Assert
        var resultado = context.Result as UnprocessableEntityObjectResult;
        var valor = resultado!.Value;
        var tipo = valor!.GetType().GetProperty("tipo")!.GetValue(valor) as string;
        var mensagem = valor.GetType().GetProperty("mensagem")!.GetValue(valor) as string;

        Assert.That(tipo, Is.EqualTo("ErroDeNegocio"));
        Assert.That(mensagem, Is.EqualTo(mensagemEsperada));
    }

    [Test]
    public void OnException_BusinessException_MarcaExceptionComoTratada()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarContexto(new BusinessException("qualquer"));

        // Act
        sut.OnException(context);

        // Assert
        Assert.That(context.ExceptionHandled, Is.True);
    }

    [Test]
    public void OnException_BusinessException_NaoLogaErro()
    {
        // Arrange — BusinessException é erro esperado, não deve aparecer como LogError.
        var sut = CriarSut();
        var context = CriarContexto(new BusinessException("dados inválidos"));

        // Act
        sut.OnException(context);

        // Assert
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    // ---- Exceção genérica em produção ----

    [Test]
    public void OnException_ExcecaoGenerica_RetornaHttp500()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarContexto(new InvalidOperationException("falha interna"));

        // Act
        sut.OnException(context);

        // Assert
        var resultado = context.Result as ObjectResult;
        Assert.That(resultado, Is.Not.Null);
        Assert.That(resultado.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public void OnException_ExcecaoGenerica_EmProducao_NaoVazaMensagemDaExcecao()
    {
        // Arrange — produção: o ex.Message NÃO pode aparecer no payload (LGPD/segurança).
        _env.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        const string mensagemInterna = "senha-root-do-banco-nao-pode-vazar";

        var sut = CriarSut();
        var context = CriarContexto(new InvalidOperationException(mensagemInterna));

        // Act
        sut.OnException(context);

        // Assert
        var resultado = context.Result as ObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(resultado!.Value);

        Assert.That(json, Does.Not.Contain(mensagemInterna));
        Assert.That(json, Does.Contain("Ocorreu um erro interno"));
    }

    [Test]
    public void OnException_ExcecaoGenerica_EmProducao_PayloadNaoContemDetalhe()
    {
        // Arrange — em produção o payload não deve ter "detalhe" nem "stackTrace".
        _env.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CriarSut();
        var context = CriarContexto(new ArgumentNullException("parametro"));

        // Act
        sut.OnException(context);

        // Assert
        var resultado = context.Result as ObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(resultado!.Value);

        Assert.That(json, Does.Not.Contain("detalhe"));
        Assert.That(json, Does.Not.Contain("stackTrace"));
        Assert.That(json, Does.Not.Contain("inner"));
    }

    [Test]
    public void OnException_ExcecaoGenerica_EmProducao_MarcaExceptionComoTratada()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarContexto(new InvalidOperationException("erro"));

        // Act
        sut.OnException(context);

        // Assert
        Assert.That(context.ExceptionHandled, Is.True);
    }

    [Test]
    public void OnException_ExcecaoGenerica_LogaErroComPath()
    {
        // Arrange
        _env.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CriarSut();
        var context = CriarContexto(new InvalidOperationException("mensagem de erro"), "/api/pacientes");

        // Act
        sut.OnException(context);

        // Assert — deve logar como erro (sem verificar conteúdo completo — estrutura do ILogger é opaca ao Moq)
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    // ---- Exceção genérica em Development ----

    [Test]
    public void OnException_ExcecaoGenerica_EmDevelopment_PayloadContemDetalhe()
    {
        // Arrange — em Development o detalhe é exposto para facilitar diagnóstico.
        _env.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        const string mensagemInterna = "connection refused ao DB";

        var sut = CriarSut();
        var context = CriarContexto(new InvalidOperationException(mensagemInterna));

        // Act
        sut.OnException(context);

        // Assert
        var resultado = context.Result as ObjectResult;
        Assert.That(resultado!.StatusCode, Is.EqualTo(500));

        var json = System.Text.Json.JsonSerializer.Serialize(resultado.Value);
        Assert.That(json, Does.Contain(mensagemInterna));
    }

    // ---- Tipo do payload (tipo = "ErroInterno") ----

    [Test]
    public void OnException_ExcecaoGenerica_PayloadContemTipoErroInterno()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarContexto(new Exception("qualquer"));

        // Act
        sut.OnException(context);

        // Assert
        var resultado = context.Result as ObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(resultado!.Value);
        Assert.That(json, Does.Contain("ErroInterno"));
    }
}
