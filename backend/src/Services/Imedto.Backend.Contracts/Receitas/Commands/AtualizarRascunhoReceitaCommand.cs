using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Commands;

/// <summary>
/// Atualiza um rascunho de receita (autosave). Substitui observações e itens
/// — não há merge incremental. Disponível apenas quando o aggregate está em
/// status Rascunho.
/// </summary>
public class AtualizarRascunhoReceitaCommand : ICommand
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string? Observacoes { get; set; }
    public List<ItemReceitaPayload> Itens { get; set; } = new();
}
