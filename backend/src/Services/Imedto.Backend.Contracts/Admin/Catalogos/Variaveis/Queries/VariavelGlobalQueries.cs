namespace Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries;

public record ListarVariaveisGlobaisQuery(
    bool IncluirInativos = false,
    string? Busca = null,
    string? Tipo = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public record ObterVariavelGlobalQuery(Guid Id);
