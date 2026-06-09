namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Fonte de verdade da política de retenção do audit admin (Wave 7).
/// Mapeia cada ação de <see cref="AcoesAuditAdmin"/> para um TTL em dias.
/// Ação não mapeada cai no <see cref="DefaultDias"/> (conservador: 365 dias).
///
/// Ao introduzir nova constante em <see cref="AcoesAuditAdmin"/>, adicionar
/// entrada explícita aqui — caso contrário o job aplica o default de 365 dias.
/// </summary>
public static class AuditLogRetencao
{
    public const int DefaultDias = 365;

    public static readonly IReadOnlyDictionary<string, int> PorAcao = new Dictionary<string, int>
    {
        // Ruído residual — backlog limpo via migration Wave 7; janela curta caso ressurjam.
        [AcoesAuditAdmin.LoginOk]            = 30,
        [AcoesAuditAdmin.Logout]             = 30,
        [AcoesAuditAdmin.AbrirDetalheTenant] = 30,

        // Forense de segurança — janela de 1 ano.
        [AcoesAuditAdmin.LoginFail]          = 365,
        [AcoesAuditAdmin.ResetSenhaPropria]  = 365,

        // LGPD: revelação de PII deve ser rastreável por 2 anos (Art. 11).
        [AcoesAuditAdmin.RevelarCpfDono]     = 730,

        // Mutações contratuais/financeiras — 2 anos (prazo prescricional padrão).
        [AcoesAuditAdmin.ResetarTenant]      = 730,
        [AcoesAuditAdmin.CriarPlano]         = 730,
        [AcoesAuditAdmin.AtualizarPlano]     = 730,
        [AcoesAuditAdmin.AtivarPlano]        = 730,
        [AcoesAuditAdmin.DesativarPlano]     = 730,
        [AcoesAuditAdmin.EditarPlano]        = 730,
        [AcoesAuditAdmin.TrocarPlano]        = 730,
        [AcoesAuditAdmin.AlterarAssinatura]  = 730,
        [AcoesAuditAdmin.ConcederGratuidade] = 730,
        [AcoesAuditAdmin.EncerrarAssinatura] = 730,
        [AcoesAuditAdmin.ResetarSenhaAdmin]  = 730,
        [AcoesAuditAdmin.CriarAdmin]         = 730,
        [AcoesAuditAdmin.DesativarAdmin]     = 730,
        [AcoesAuditAdmin.ReativarAdmin]      = 730,
        [AcoesAuditAdmin.AtualizarConfig]    = 730,

        // Catálogos padrão-sistema — 1 ano (raramente disputado; estado atual está no catálogo).
        [AcoesAuditAdmin.CriarModeloPadraoSistema]      = 365,
        [AcoesAuditAdmin.AtualizarModeloPadraoSistema]  = 365,
        [AcoesAuditAdmin.InativarModeloPadraoSistema]   = 365,
        [AcoesAuditAdmin.ReativarModeloPadraoSistema]   = 365,
        [AcoesAuditAdmin.CriarVariavelPadraoSistema]    = 365,
        [AcoesAuditAdmin.AtualizarVariavelPadraoSistema] = 365,
        [AcoesAuditAdmin.InativarVariavelPadraoSistema] = 365,
        [AcoesAuditAdmin.ReativarVariavelPadraoSistema] = 365,
        [AcoesAuditAdmin.CriarRegiaoAnatomica]          = 365,
        [AcoesAuditAdmin.AtualizarRegiaoAnatomica]      = 365,
        [AcoesAuditAdmin.InativarRegiaoAnatomica]       = 365,
        [AcoesAuditAdmin.ReativarRegiaoAnatomica]       = 365,
        [AcoesAuditAdmin.ExcluirRegiaoAnatomica]        = 365,

        // Briefing 2026-06-04_001 — Modelos de permissão padrão sistema
        [AcoesAuditAdmin.CriarModeloPermissaoPadraoSistema]    = 365,
        [AcoesAuditAdmin.AtualizarModeloPermissaoPadraoSistema] = 365,
        [AcoesAuditAdmin.ExcluirModeloPermissaoPadraoSistema]  = 365,
    };

    /// <summary>
    /// Retorna o TTL em dias para a ação informada.
    /// Ação não mapeada retorna <see cref="DefaultDias"/>.
    /// </summary>
    public static int TtlDiasParaAcao(string acao)
        => PorAcao.TryGetValue(acao, out var dias) ? dias : DefaultDias;
}
