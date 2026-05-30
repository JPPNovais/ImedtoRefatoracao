namespace Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries;

public record ListarRegioesGlobaisQuery(
    bool IncluirInativos = false,
    string? Busca = null,
    string? SistemaCorporal = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public record ObterRegiaoGlobalQuery(Guid Id);
