using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.AssinaturaDigital.Commands;

/// <summary>
/// Remove o vínculo do certificado do médico autenticado.
/// Receitas já em AssinaturaPendente continuam aguardando o webhook.
/// </summary>
public class RemoverCertificadoCommand : ICommand
{
    public Guid MedicoId { get; set; }
}
