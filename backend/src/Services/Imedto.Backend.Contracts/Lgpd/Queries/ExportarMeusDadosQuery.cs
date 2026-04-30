using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Lgpd.Queries;

public class ExportarMeusDadosQuery : IQuery<MeusDadosLgpdDto>
{
    public Guid UsuarioId { get; init; }
}
