using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Profissional pede acesso a um estabelecimento (fluxo inverso ao convite).
/// Não exige <c>RequiresEstabelecimento</c> — o profissional ainda não é vinculado.
/// </summary>
public class SolicitarVinculoCommand : ICommand
{
    public Guid ProfissionalUsuarioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Mensagem { get; set; }
}
