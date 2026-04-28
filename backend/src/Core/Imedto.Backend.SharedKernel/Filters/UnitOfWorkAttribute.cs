using Microsoft.AspNetCore.Mvc.Filters;

namespace Imedto.Backend.SharedKernel.Filters;

/// <summary>
/// Action filter que abre uma transação por requisição e faz commit se a action não lançou exceção.
/// Implementa IAsyncActionFilter diretamente (não herda de Attribute) para que o ASP.NET Core
/// o resolva via TypeFilter/ActivatorUtilities — uma única instância por request.
/// Registrado globalmente em Program.cs como options.Filters.Add&lt;UnitOfWorkFilter&gt;().
/// </summary>
public class UnitOfWorkFilter : IAsyncActionFilter
{
    private readonly IUnitOfWorkFactory _uofwFactory;

    public UnitOfWorkFilter(IUnitOfWorkFactory uofwFactory)
    {
        _uofwFactory = uofwFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await using var scope = _uofwFactory.Begin();
        var result = await next();

        if (result.Exception is null)
            await scope.CommitAsync();
    }
}
