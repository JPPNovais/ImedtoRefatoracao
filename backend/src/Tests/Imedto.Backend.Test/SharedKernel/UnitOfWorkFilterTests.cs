using Imedto.Backend.SharedKernel.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.SharedKernel;

[TestFixture]
public class UnitOfWorkFilterTests
{
    private Mock<IUnitOfWorkFactory> _factory;
    private Mock<IUnitOfWorkScope> _scope;

    [SetUp]
    public void SetUp()
    {
        _scope = new Mock<IUnitOfWorkScope>();
        _scope
            .Setup(s => s.CommitAsync())
            .Returns(Task.CompletedTask);
        _scope
            .Setup(s => s.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _factory = new Mock<IUnitOfWorkFactory>();
        _factory
            .Setup(f => f.Begin())
            .Returns(_scope.Object);
    }

    private UnitOfWorkFilter CriarSut() =>
        new UnitOfWorkFilter(_factory.Object);

    private static ActionExecutingContext CriarExecutingContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: new object());
    }

    /// <summary>
    /// Cria um delegate que simula a action retornando sem exceção.
    /// </summary>
    private static ActionExecutionDelegate CriarDelegateSemExcecao()
    {
        return () =>
        {
            var ctx = CriarExecutingContext();
            var executedCtx = new ActionExecutedContext(ctx, new List<IFilterMetadata>(), controller: new object());
            // Exception = null significa que a action concluiu sem erro.
            return Task.FromResult(executedCtx);
        };
    }

    /// <summary>
    /// Cria um delegate que simula a action terminando com exceção não-tratada.
    /// </summary>
    private static ActionExecutionDelegate CriarDelegateComExcecao(Exception ex)
    {
        return () =>
        {
            var ctx = CriarExecutingContext();
            var executedCtx = new ActionExecutedContext(ctx, new List<IFilterMetadata>(), controller: new object())
            {
                Exception = ex,
                ExceptionHandled = false
            };
            return Task.FromResult(executedCtx);
        };
    }

    // ---- CommitAsync ----

    [Test]
    public async Task OnActionExecutionAsync_ActionSemExcecao_ChamaCommitAsync()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarExecutingContext();

        // Act
        await sut.OnActionExecutionAsync(context, CriarDelegateSemExcecao());

        // Assert
        _scope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_ActionComExcecao_NaoChamaCommitAsync()
    {
        // Arrange — rollback implícito: exceção não-nula no result → commit não ocorre.
        var sut = CriarSut();
        var context = CriarExecutingContext();
        var excecao = new InvalidOperationException("falha na action");

        // Act
        await sut.OnActionExecutionAsync(context, CriarDelegateComExcecao(excecao));

        // Assert
        _scope.Verify(s => s.CommitAsync(), Times.Never);
    }

    // ---- DisposeAsync ----

    [Test]
    public async Task OnActionExecutionAsync_ActionSemExcecao_SempreChamamDisposeAsync()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarExecutingContext();

        // Act
        await sut.OnActionExecutionAsync(context, CriarDelegateSemExcecao());

        // Assert — o await using garante DisposeAsync independente do caminho.
        _scope.Verify(s => s.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_ActionComExcecao_SempreChamamDisposeAsync()
    {
        // Arrange — mesmo com exceção, DisposeAsync (rollback implícito) deve ser chamado.
        var sut = CriarSut();
        var context = CriarExecutingContext();

        // Act
        await sut.OnActionExecutionAsync(context, CriarDelegateComExcecao(new Exception("erro")));

        // Assert
        _scope.Verify(s => s.DisposeAsync(), Times.Once);
    }

    // ---- Begin ----

    [Test]
    public async Task OnActionExecutionAsync_SempreAbreScope()
    {
        // Arrange
        var sut = CriarSut();
        var context = CriarExecutingContext();

        // Act
        await sut.OnActionExecutionAsync(context, CriarDelegateSemExcecao());

        // Assert
        _factory.Verify(f => f.Begin(), Times.Once);
    }

    // ---- Ordenação commit/dispose ----

    [Test]
    public async Task OnActionExecutionAsync_CommitOcorreAntesDoDispose()
    {
        // Arrange — garante que a transação é comitada antes de ser descartada.
        var ordemDeChamadas = new List<string>();

        _scope.Setup(s => s.CommitAsync())
            .Callback(() => ordemDeChamadas.Add("commit"))
            .Returns(Task.CompletedTask);

        _scope.Setup(s => s.DisposeAsync())
            .Callback(() => ordemDeChamadas.Add("dispose"))
            .Returns(ValueTask.CompletedTask);

        var sut = CriarSut();
        var context = CriarExecutingContext();

        // Act
        await sut.OnActionExecutionAsync(context, CriarDelegateSemExcecao());

        // Assert
        Assert.That(ordemDeChamadas, Is.EqualTo(new[] { "commit", "dispose" }));
    }
}
