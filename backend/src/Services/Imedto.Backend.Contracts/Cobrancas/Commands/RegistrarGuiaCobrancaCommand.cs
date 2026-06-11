using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Commands;

/// <summary>
/// Registra ou edita dados de guia/autorização na cobrança convênio (F6/R10/R13).
/// RBAC: financeiro_paciente.registrar.
/// </summary>
public class RegistrarGuiaCobrancaCommand : ICommand
{
    public long CobrancaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string GuiaNumero { get; set; } = string.Empty;
    public string? GuiaSenha { get; set; }
    public DateOnly? GuiaAutorizadaEm { get; set; }
}
