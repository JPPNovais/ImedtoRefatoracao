namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Estado clínico da receita. Note a diferença para <c>ISoftDeletable</c>:
/// <list type="bullet">
///   <item><see cref="Rascunho"/> é estado de trabalho — receita ainda não foi
///   "publicada" ao paciente; aceita autosave de observações/itens. Não consta
///   em audit de Escrita até virar <see cref="Emitida"/>.</item>
///   <item><see cref="Emitida"/> — receita finalizada, válida clinicamente. A
///   partir daqui não dá mais para editar conteúdo (só Cancelar/Substituir).</item>
///   <item><see cref="Cancelada"/> é estado clínico — paciente vê erro de prescrição,
///   exige motivo, fica visível no histórico para auditoria/correção médica.</item>
///   <item>Soft delete (<c>DeletadoEm</c>) é remoção lógica do registro pelo profissional
///   (ex.: receita criada por engano fora do contexto certo) — some das listagens.</item>
/// </list>
/// </summary>
public enum StatusReceita
{
    Rascunho,
    Emitida,
    Cancelada,
    /// <summary>Substituída por uma nova receita (ex.: troca de dosagem).</summary>
    Substituida
}
