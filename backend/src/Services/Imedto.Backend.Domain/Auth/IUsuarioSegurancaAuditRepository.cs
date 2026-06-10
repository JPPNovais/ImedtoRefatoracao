namespace Imedto.Backend.Domain.Auth;

/// <summary>Repositório append-only de auditoria de segurança de conta (R13).</summary>
public interface IUsuarioSegurancaAuditRepository
{
    Task Adicionar(UsuarioSegurancaAudit auditoria);
}
