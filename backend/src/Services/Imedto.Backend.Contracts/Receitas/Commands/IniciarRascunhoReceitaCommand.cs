using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Commands;

/// <summary>
/// Inicia uma receita em rascunho. Itens podem ser vazios (autosave preenche
/// depois via <see cref="AtualizarRascunhoReceitaCommand"/>). LGPD: rascunho
/// não consta como Escrita em audit — só vira escrita quando finalizar.
/// </summary>
public class IniciarRascunhoReceitaCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    /// <summary>"Comum" | "Controlada" | "Antibiotico" | "Especial".</summary>
    public string Tipo { get; set; } = "Comum";
    /// <summary>"A" | "B" | "C" | "Especial". Mesma regra do Emitir — obrigatório se Controlada.</summary>
    public string? TipoNotificacao { get; set; }
    public DateTime? ValidadeAte { get; set; }
    public string? Observacoes { get; set; }
    public List<ItemReceitaPayload> Itens { get; set; } = new();

    /// <summary>Preenchido pelo handler — id do rascunho criado.</summary>
    public long ReceitaIdCriada { get; set; }
}
