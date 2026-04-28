using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>Atribui (ou troca) o modelo de permissão de um vínculo profissional ativo.</summary>
public class AlterarModeloPermissaoDoVinculoCommand : ICommand
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long NovoModeloPermissaoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
