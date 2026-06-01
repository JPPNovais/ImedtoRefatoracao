namespace Imedto.Backend.Domain.AssinaturaDigital;

/// <summary>
/// Repositório de escrita para <see cref="AssinaturaCertificado"/>.
/// O refresh token retornado em <see cref="ObterPorMedicoAsync"/> está CIFRADO —
/// o provider decifra via IDataProtectionProvider antes de usar.
/// </summary>
public interface IAssinaturaCertificadoRepository
{
    /// <summary>
    /// Retorna o certificado vinculado ao médico (qualquer provedor).
    /// Null se não houver vínculo.
    /// </summary>
    Task<AssinaturaCertificado?> ObterPorMedicoAsync(Guid medicoId, CancellationToken ct = default);

    Task Salvar(AssinaturaCertificado certificado);
    Task Remover(AssinaturaCertificado certificado);
}
