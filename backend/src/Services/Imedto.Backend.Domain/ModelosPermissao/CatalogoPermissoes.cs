namespace Imedto.Backend.Domain.ModelosPermissao;

/// <summary>
/// Catálogo de áreas e ações granulares de permissão (formato <c>area.acao</c>).
///
/// Este catálogo é a fonte de verdade do que pode ser concedido em um modelo. As chaves
/// aqui definidas devem casar com o catálogo do frontend em
/// <c>frontend/src/constants/permissions.ts</c> (PERMISSION_AREAS).
///
/// Convenção: identificadores em PT-BR (consistente com o resto do domínio).
///
/// As permissões são usadas pelo frontend para decidir se mostra/oculta itens de menu e
/// ações. O backend usa <see cref="PermissoesExtras"/> via <c>RequiresPermissaoExtraAttribute</c>
/// para gates de endpoints sensíveis (gerir permissões, IA etc.) — as ações por área são
/// UX-only e devem ser espelhadas em validações de domínio quando relevantes.
/// </summary>
public static class CatalogoPermissoes
{
    public sealed record Area(string Chave, IReadOnlyList<string> Acoes);

    public static readonly IReadOnlyList<Area> Areas = new[]
    {
        new Area("agenda",        new[] { "ver", "criar", "editar", "excluir" }),
        new Area("prontuario",    new[] { "ver", "editar", "assinar" }),
        new Area("prescricao",    new[] { "criar", "assinar" }),
        new Area("pacientes",     new[] { "ver", "criar", "editar", "excluir" }),
        new Area("financeiro",          new[] { "ver", "lancar", "fechar" }),
        new Area("financeiro_paciente", new[] { "ver", "registrar" }),
        new Area("orcamento",     new[] { "ver", "criar", "editar", "aprovar", "configurar" }),
        new Area("convenios",     new[] { "ver", "gerenciar" }),
        new Area("estoque",       new[] { "ver", "gerenciar" }),
        new Area("relatorios",    new[] { "ver", "exportar" }),
        new Area("configuracoes", new[] { "gerenciar" }),
        new Area("equipe",        new[] { "ver", "convidar", "permissoes", "remover" }),
        new Area("termos",        new[] { "emitir", "gerenciar_modelos" }),
    };

    /// <summary>Todas as permissões granulares (catálogo completo).</summary>
    public static readonly IReadOnlyList<string> Todas =
        Areas.SelectMany(a => a.Acoes.Select(ac => $"{a.Chave}.{ac}")).ToList();

    /// <summary>Conjunto padrão para o papel Admin (acesso total).</summary>
    public static readonly IReadOnlyList<string> AdminPadrao = Todas;

    /// <summary>Conjunto padrão para o papel Médico (agenda + prontuário + receitas + leitura/edição de pacientes).</summary>
    public static readonly IReadOnlyList<string> MedicoPadrao = new[]
    {
        "agenda.ver", "agenda.criar", "agenda.editar", "agenda.excluir",
        "prontuario.ver", "prontuario.editar", "prontuario.assinar",
        "prescricao.criar", "prescricao.assinar",
        "pacientes.ver", "pacientes.criar", "pacientes.editar",
        "orcamento.ver", "orcamento.criar", "orcamento.editar",
        "relatorios.ver",
        "termos.emitir",
    };

    /// <summary>Conjunto padrão para o papel Recepção (agenda + cadastro de pacientes + leitura financeiro + registrar pagamento).</summary>
    public static readonly IReadOnlyList<string> RecepcaoPadrao = new[]
    {
        "agenda.ver", "agenda.criar", "agenda.editar", "agenda.excluir",
        "pacientes.ver", "pacientes.criar", "pacientes.editar",
        "prontuario.ver",
        "convenios.ver",
        "financeiro.ver",
        "financeiro_paciente.ver", "financeiro_paciente.registrar",
        "termos.emitir",
    };

    /// <summary>
    /// Mapeia chaves legadas do legado Imedto (sem ponto, em PT-BR) para a área equivalente
    /// no catálogo novo. Usado na migração para converter modelos antigos.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> MapeamentoLegado =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["agenda"] = "agenda",
            ["pacientes"] = "pacientes",
            ["prontuario"] = "prontuario",
            ["orcamentos"] = "orcamento",
            ["estoque"] = "estoque",
            ["financeiro"] = "financeiro",
            ["relatorios"] = "relatorios",
            ["convenios"] = "convenios",
            // perfil_profissional, minhas_consultas, home: não são áreas controláveis,
            // são telas pessoais sempre acessíveis para qualquer profissional vinculado.
        };

    /// <summary>
    /// Expande uma chave legada (sem ponto) para todas as ações da área correspondente.
    /// Retorna lista vazia para chaves não mapeáveis (ex: <c>"home"</c>, <c>"perfil_profissional"</c>).
    /// </summary>
    public static IReadOnlyList<string> ExpandirChaveLegada(string chaveLegada)
    {
        if (string.IsNullOrWhiteSpace(chaveLegada)) return Array.Empty<string>();
        if (!MapeamentoLegado.TryGetValue(chaveLegada.Trim(), out var areaNova))
            return Array.Empty<string>();

        var area = Areas.FirstOrDefault(a => a.Chave.Equals(areaNova, StringComparison.Ordinal));
        if (area is null) return Array.Empty<string>();
        return area.Acoes.Select(ac => $"{area.Chave}.{ac}").ToList();
    }
}
