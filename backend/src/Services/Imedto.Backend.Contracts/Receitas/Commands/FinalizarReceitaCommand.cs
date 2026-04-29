using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Commands;

/// <summary>
/// Finaliza uma receita em rascunho — vira <c>Emitida</c>. Aplica regras
/// clínicas que rascunho não exige (≥1 item, controlada exige validade futura).
/// LGPD: este é o ponto em que registramos audit de Escrita e atualizamos
/// ranking de medicamentos favoritos.
/// </summary>
public class FinalizarReceitaCommand : ICommand
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
