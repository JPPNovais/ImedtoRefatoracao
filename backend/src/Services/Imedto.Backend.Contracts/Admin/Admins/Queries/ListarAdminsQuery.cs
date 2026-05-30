using Imedto.Backend.Contracts.Admin.Admins.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Admin.Admins.Queries;

public record ListarAdminsQuery(
    string Busca,
    int Pagina,
    int Tamanho) : IQuery<ListarAdminsResult>;

public record ObterAdminQuery(Guid Id) : IQuery<AdminDetalheDto>;
