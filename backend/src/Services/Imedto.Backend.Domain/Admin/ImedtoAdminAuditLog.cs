using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Log de auditoria de ações do administrador global. Append-only — nunca atualizar ou deletar
/// registros. Retenção de 2 anos documentada em Docs/LGPD.md.
///
/// Ações registradas: toda mutação admin e leitura de detalhe individual de tenant.
/// Listagens NÃO geram audit (volume sem valor forense).
/// </summary>
public class ImedtoAdminAuditLog : Entity<Guid>
{
    /// <summary>Admin que executou a ação. Nullable para LOGIN_FAIL (admin pode não existir).</summary>
    public virtual Guid? AdminId { get; protected set; }

    /// <summary>Código da ação. Ver constantes em <see cref="AcoesAuditAdmin"/>.</summary>
    public virtual string Acao { get; protected set; } = string.Empty;

    /// <summary>Tipo do recurso afetado: "estabelecimento", "plano", "assinatura", "admin", "config".</summary>
    public virtual string? RecursoTipo { get; protected set; }

    /// <summary>ID do recurso afetado (text para aceitar uuid e bigint).</summary>
    public virtual string? RecursoId { get; protected set; }

    /// <summary>ID do estabelecimento afetado (quando a ação afeta um tenant específico).</summary>
    public virtual long? TenantAfetadoId { get; protected set; }

    /// <summary>Motivo obrigatório para operações destrutivas. Opcional para ações informativas.</summary>
    public virtual string? Motivo { get; protected set; }

    public virtual string? Ip { get; protected set; }
    public virtual string? UserAgent { get; protected set; }

    /// <summary>Payload resumido sem PII. Ex: {"plano_antigo":"X","plano_novo":"Y"}.</summary>
    public virtual string? PayloadJson { get; protected set; }

    public virtual DateTimeOffset CriadoEm { get; protected set; }

    protected ImedtoAdminAuditLog() { }

    public static ImedtoAdminAuditLog Registrar(
        string acao,
        Guid? adminId = null,
        string? recursoTipo = null,
        string? recursoId = null,
        long? tenantAfetadoId = null,
        string? motivo = null,
        string? ip = null,
        string? userAgent = null,
        string? payloadJson = null)
    {
        if (string.IsNullOrWhiteSpace(acao))
            throw new ArgumentException("Ação é obrigatória.", nameof(acao));

        return new ImedtoAdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Acao = acao,
            RecursoTipo = recursoTipo,
            RecursoId = recursoId,
            TenantAfetadoId = tenantAfetadoId,
            Motivo = motivo,
            Ip = ip,
            UserAgent = userAgent,
            PayloadJson = payloadJson,
            CriadoEm = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// Constantes de ação para <see cref="ImedtoAdminAuditLog.Acao"/>.
/// Usar estas constantes em vez de strings literais nos handlers.
/// </summary>
public static class AcoesAuditAdmin
{
    public const string LoginOk = "LOGIN_OK";
    public const string LoginFail = "LOGIN_FAIL";
    public const string Logout = "LOGOUT";
    public const string CriarAdmin = "CRIAR_ADMIN";
    public const string DesativarAdmin = "DESATIVAR_ADMIN";
    public const string ReativarAdmin = "REATIVAR_ADMIN";
    public const string ResetarSenhaAdmin = "RESETAR_SENHA_ADMIN";
    public const string ResetSenhaAdmin = "RESETAR_SENHA_ADMIN";
    public const string AbrirDetalheTenant = "ABRIR_DETALHE_TENANT";
    public const string RevelarCpfDono = "REVELAR_CPF_DONO";
    public const string ResetarTenant = "RESETAR_TENANT";
    public const string CriarPlano = "CRIAR_PLANO";
    public const string AtualizarPlano = "ATUALIZAR_PLANO";
    public const string AtivarPlano = "ATIVAR_PLANO";
    public const string DesativarPlano = "DESATIVAR_PLANO";
    public const string EditarPlano = "EDITAR_PLANO";
    public const string TrocarPlano = "TROCAR_PLANO";
    public const string AlterarAssinatura = "ALTERAR_ASSINATURA";
    public const string ConcederGratuidade = "CONCEDER_GRATUIDADE";
    public const string EncerrarAssinatura = "ENCERRAR_ASSINATURA";
    public const string ResetSenhaPropria = "RESET_SENHA_PROPRIA";

    // Frente 2 — Configs globais
    public const string AtualizarConfig = "ATUALIZAR_CONFIG";

    // Frente 3 — Catálogos globais (modelos de prontuário)
    public const string CriarModeloGlobal = "CRIAR_MODELO_PRONTUARIO_GLOBAL";
    public const string AtualizarModeloGlobal = "ATUALIZAR_MODELO_PRONTUARIO_GLOBAL";
    public const string DesativarModeloGlobal = "DESATIVAR_MODELO_PRONTUARIO_GLOBAL";
    public const string ReativarModeloGlobal = "REATIVAR_MODELO_PRONTUARIO_GLOBAL";

    // Frente 3 — Catálogos globais (variáveis pool)
    public const string CriarVariavelGlobal = "CRIAR_VARIAVEL_POOL_GLOBAL";
    public const string AtualizarVariavelGlobal = "ATUALIZAR_VARIAVEL_POOL_GLOBAL";
    public const string DesativarVariavelGlobal = "DESATIVAR_VARIAVEL_POOL_GLOBAL";
    public const string ReativarVariavelGlobal = "REATIVAR_VARIAVEL_POOL_GLOBAL";

    // Frente 3 — Catálogos globais (regiões anatômicas)
    public const string CriarRegiaoGlobal = "CRIAR_REGIAO_ANATOMICA_GLOBAL";
    public const string AtualizarRegiaoGlobal = "ATUALIZAR_REGIAO_ANATOMICA_GLOBAL";
    public const string DesativarRegiaoGlobal = "DESATIVAR_REGIAO_ANATOMICA_GLOBAL";
    public const string ReativarRegiaoGlobal = "REATIVAR_REGIAO_ANATOMICA_GLOBAL";
}
