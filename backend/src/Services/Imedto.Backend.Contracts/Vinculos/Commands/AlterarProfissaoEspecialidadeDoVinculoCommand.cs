using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Altera a profissão e a especialidade do vínculo atomicamente.
/// Trocar a profissão sempre limpa a especialidade — separar em dois comandos
/// criaria janela de estado inconsistente (profissão nova + especialidade da profissão antiga).
/// Especialidade nula/vazia limpa o campo — exibição passa a usar o cadastro global.
/// </summary>
public class AlterarProfissaoEspecialidadeDoVinculoCommand : ICommand
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>
    /// Id da profissão do catálogo. Obrigatório quando <see cref="Especialidade"/> for informada.
    /// Nulo limpa profissao_convidada_id do vínculo (volta ao cadastro global).
    /// </summary>
    public long? ProfissaoId { get; set; }
    /// <summary>Nulo ou vazio limpa a especialidade do vínculo (fallback para cadastro global).</summary>
    public string? Especialidade { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
