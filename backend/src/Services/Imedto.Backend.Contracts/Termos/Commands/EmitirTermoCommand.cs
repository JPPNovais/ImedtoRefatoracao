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
    /// <summary>
    /// Quando informado, deve corresponder a um profissional com vínculo Ativo no estabelecimento.
    /// É esse usuário que aparece nas variáveis <c>{{profissional.*}}</c> do snapshot.
    /// Quando ausente (null/Guid.Empty), as variáveis caem para o fallback <c>___________</c>.
    /// O emissor (recepção/Dono administrativo) <b>não</b> é tratado automaticamente como profissional.
    /// </summary>
    public Guid? ProfissionalUsuarioId { get; set; }
    public long ModeloId { get; set; }
    /// <summary>"pdf_anexado" | "aceite_link".</summary>
    public string AssinaturaTipo { get; set; } = "pdf_anexado";

    /// <summary>
    /// Para <c>aceite_link</c>: "email" (default — dispara envio) ou "copia"
    /// (não envia, só devolve o token pra mostrar ao emissor). Ignorado em
    /// <c>pdf_anexado</c>.
    /// </summary>
    public string CanalEnvio { get; set; } = "email";

    /// <summary>Preenchido pelo handler — id do termo emitido.</summary>
    public long TermoEmitidoId { get; set; }
    /// <summary>Preenchido pelo handler quando assinatura_tipo = aceite_link.</summary>
    public string TokenAceiteGerado { get; set; }
}
