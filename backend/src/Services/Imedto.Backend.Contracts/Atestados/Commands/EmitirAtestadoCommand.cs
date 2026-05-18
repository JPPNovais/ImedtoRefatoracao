using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Atestados.Commands;

/// <summary>
/// Emite um novo atestado. As validações vivem no aggregate (Afastamento exige
/// dias > 0, CID-10 com regex, conteúdo não vazio).
/// </summary>
public class EmitirAtestadoCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    /// <summary>"Afastamento" | "Comparecimento" | "Aptidao" | "Outro".</summary>
    public string Tipo { get; set; } = "Comparecimento";
    public int? DiasAfastamento { get; set; }
    public string? Cid10 { get; set; }
    public string Conteudo { get; set; } = string.Empty;

    /// <summary>Preenchido pelo handler — id do atestado criado.</summary>
    public long AtestadoIdCriado { get; set; }
}
