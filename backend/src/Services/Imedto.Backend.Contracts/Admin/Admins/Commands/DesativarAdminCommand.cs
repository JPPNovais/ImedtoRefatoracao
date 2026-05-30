using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Admin.Admins.Commands;

public record DesativarAdminCommand(
    Guid AdminSolicitanteId,
    Guid AdminAlvoId,
    string Motivo) : ICommand;

public record ReativarAdminCommand(
    Guid AdminSolicitanteId,
    Guid AdminAlvoId,
    string Motivo) : ICommand;

public record ResetSenhaAdminCommand(
    Guid AdminSolicitanteId,
    Guid AdminAlvoId,
    string Motivo) : ICommand;
