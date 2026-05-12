namespace Imedto.Backend.Domain.ModelosPermissao;

/// <summary>
/// Catálogo de permissões finas (granulares) suportadas por <see cref="ModeloPermissaoEstabelecimento"/>.
///
/// O legado mantinha um único array <c>permissoes</c> com 13 chaves misturando "áreas"
/// (visão de módulo) e "permissões finas" (acesso a feature sensível dentro de um módulo).
/// O novo modelo separa em duas colunas — <c>permissoes</c> (áreas) e <c>permissoes_extras</c>
/// (finas). Esta classe é o catálogo canônico das chaves finas.
///
/// Mapeamento legado → novo:
///   permissoes (área):  agenda, pacientes, prontuario, orcamentos, estoque,
///                       financeiro, relatorios, minhas_consultas, perfil_profissional, home
///   permissoes_extras (fina):
///     - assistente_clinico    → <see cref="AssistenteClinicoIa"/>
///     - permissoes            → <see cref="GerirPermissoes"/>
///     - config_estabelecimento→ <see cref="ConfigEstabelecimento"/>
///     - profissionais         → <see cref="GerirProfissionais"/>
///     - modelos_prontuario    → <see cref="ModelosProntuario"/>
///     - automacao             → <see cref="AutomacaoConfig"/>
///
/// Use as constantes — nunca strings literais — para evitar typos e facilitar refactor.
/// Cada item desta lista é uma chave estável que o backend valida via
/// <c>IModeloPermissaoRepository.UsuarioTemPermissaoExtra</c> (dono sempre passa).
/// </summary>
public static class PermissoesExtras
{
    /// <summary>
    /// Habilita o assistente clínico de IA (sugestão de seções de prontuário, evolução etc.)
    /// para profissionais vinculados via este modelo. Trava aplicada em <c>RateLimitedIaService</c>.
    /// Equivalente legado: <c>assistente_clinico</c>.
    /// </summary>
    public const string AssistenteClinicoIa = "ia_assistente_clinico";

    /// <summary>
    /// Permite editar modelos de permissão e atribuí-los a profissionais — tela
    /// "Gestão de permissões" do legado. Aplicar em <c>ModeloPermissaoController</c> em
    /// vez de exigir <c>TenantPapel.Dono</c>, para permitir que o dono delegue a um gerente.
    /// Equivalente legado: <c>permissoes</c>.
    /// </summary>
    public const string GerirPermissoes = "gerir_permissoes";

    /// <summary>
    /// Permite editar dados/configurações do estabelecimento (funcionamento, horários,
    /// bloqueios, foto/logo). Equivalente legado: <c>config_estabelecimento</c>.
    /// </summary>
    public const string ConfigEstabelecimento = "config_estabelecimento";

    /// <summary>
    /// Permite convidar/remover profissionais e gerenciar vínculos do estabelecimento —
    /// tela "Gestão de profissionais" do legado. Equivalente legado: <c>profissionais</c>.
    /// </summary>
    public const string GerirProfissionais = "gerir_profissionais";

    /// <summary>
    /// Permite criar/editar templates de prontuário (estrutura e seções). Não confundir
    /// com a permissão de área <c>prontuario</c>, que dá acesso de leitura/escrita ao
    /// prontuário do paciente em si. Equivalente legado: <c>modelos_prontuario</c>.
    /// </summary>
    public const string ModelosProntuario = "modelos_prontuario";

    /// <summary>
    /// Permite criar/editar regras de automação (gatilhos, ações, configuração).
    /// Listagem permanece aberta a membros do tenant. Equivalente legado: <c>automacao</c>.
    /// </summary>
    public const string AutomacaoConfig = "automacao_config";

    /// <summary>
    /// Catálogo completo — usado em validações no domain pra rejeitar chaves
    /// arbitrárias enviadas pelo cliente.
    /// </summary>
    public static readonly IReadOnlyList<string> Todas = new[]
    {
        AssistenteClinicoIa,
        GerirPermissoes,
        ConfigEstabelecimento,
        GerirProfissionais,
        ModelosProntuario,
        AutomacaoConfig,
    };
}
