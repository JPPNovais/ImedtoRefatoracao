using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Emite um termo para um paciente. O handler resolve variáveis, sanitiza o resultado,
/// calcula hash de integridade e (quando <see cref="AssinaturaTipo"/> = "aceite_link")
/// gera um token público de aceite.
/// </summary>
public class EmitirTermoCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid EmissorUsuarioId { get; set; }
    public long ModeloId { get; set; }
    /// <summary>"pdf_anexado" | "aceite_link".</summary>
    public string AssinaturaTipo { get; set; } = "pdf_anexado";

    /// <summary>Preenchido pelo handler — id do termo emitido.</summary>
    public long TermoEmitidoId { get; set; }
    /// <summary>Preenchido pelo handler quando assinatura_tipo = aceite_link.</summary>
    public string TokenAceiteGerado { get; set; }
}
