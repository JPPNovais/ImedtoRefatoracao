using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Define (ou limpa) a especialidade do vínculo para este estabelecimento.
/// Especialidade nula/vazia limpa o campo — exibição passa a usar o cadastro global do profissional.
/// </summary>
public class AlterarEspecialidadeDoVinculoCommand : ICommand
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Nulo ou vazio limpa a especialidade do vínculo (fallback para cadastro global).</summary>
    public string? Especialidade { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
