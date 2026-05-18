using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.PedidosExame.Commands;

public class EmitirPedidoExameCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    /// <summary>"Laboratorial" | "Imagem" | "Misto".</summary>
    public string Tipo { get; set; } = "Laboratorial";
    public List<string> Exames { get; set; } = new();
    public string IndicacaoClinica { get; set; } = string.Empty;
    public string? Cid10 { get; set; }
    public string? Observacoes { get; set; }

    public long PedidoExameIdCriado { get; set; }
}
