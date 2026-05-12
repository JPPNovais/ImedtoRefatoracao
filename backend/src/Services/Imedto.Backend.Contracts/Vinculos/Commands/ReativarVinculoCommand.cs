using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Reativa um vínculo inativado, devolvendo acesso imediato ao profissional
/// (sem novo convite). Só o Dono do estabelecimento pode reativar.
/// </summary>
public class ReativarVinculoCommand : ICommand
{
    public long VinculoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
