namespace Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries;

public record ListarModelosGlobaisQuery(
    bool IncluirInativos = false,
    string? Busca = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public record ObterModeloGlobalQuery(Guid Id);
