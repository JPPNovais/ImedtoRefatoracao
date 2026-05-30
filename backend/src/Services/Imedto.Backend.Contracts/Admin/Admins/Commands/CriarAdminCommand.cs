using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Admin.Admins.Commands;

public record CriarAdminCommand(
    Guid AdminSolicitanteId,
    string Nome,
    string Email,
    string Motivo) : ICommand;
