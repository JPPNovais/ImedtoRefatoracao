namespace Imedto.Backend.Contracts.Admin.Planos.Queries;

public record ListarPlanosAdminQuery(
    bool? Ativo,
    string? Busca,
    int Pagina = 1,
    int Tamanho = 25);
