using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Reativa um vínculo inativado, devolvendo acesso imediato ao profissional
/// (sem novo convite). Dono ou usuário com permissão <c>gerir_profissionais</c>
/// no estabelecimento podem reativar.
/// </summary>
public class ReativarVinculoCommand : ICommand
{
    public long VinculoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
