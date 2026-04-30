namespace Imedto.Backend.Domain.Lgpd;

public interface ILgpdConsentimentoRepository
{
    Task Salvar(LgpdConsentimento consentimento);
    Task<IEnumerable<LgpdConsentimento>> ListarPorUsuario(Guid usuarioId);
}
