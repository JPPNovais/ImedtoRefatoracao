using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Admin.Estabelecimentos.Commands;

/// <summary>
/// Reseta dados do tenant. Confirmação dupla obrigatória (CA32–CA36):
/// - <see cref="ConfirmarNomeFantasia"/> deve bater exatamente (case-insensitive) com o nome real.
/// - <see cref="Motivo"/> obrigatório (mín. 10 chars).
/// Gera audit <c>ResetTenant</c>. Chama <see cref="IAdminResetService"/>.
/// </summary>
public class ResetTenantCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid AdminId { get; set; }
    public string ConfirmarNomeFantasia { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}
