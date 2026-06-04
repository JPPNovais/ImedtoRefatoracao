using System.Text.Json;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.ModelosPermissao;

/// <summary>
/// Template de permissões atribuído a profissionais de um estabelecimento.
///
/// <see cref="TipoAcesso"/> mantém o papel base (Profissional ou Recepcionista) — usado
/// por filtros de tenancy.
///
/// <see cref="Permissoes"/> guarda as permissões granulares no formato <c>area.acao</c>
/// (ex: <c>agenda.view</c>, <c>agenda.create</c>, <c>patients.edit</c>). Para retrocompatibilidade
/// com modelos antigos do legado, chaves sem ponto (ex: <c>"agenda"</c>) também são aceitas e
/// representam acesso total à área — ver <see cref="TemAreaCompleta"/>.
///
/// <see cref="PermissoesExtrasLista"/> guarda permissões finas por feature (catálogo em
/// <see cref="PermissoesExtras"/>) — ex: assistente clínico de IA, emissão de receitas.
///
/// <see cref="Icone"/>, <see cref="Cor"/> e <see cref="Descricao"/> são metadados visuais
/// usados pelo frontend para decorar o papel (selector de papel, pill no header etc.).
/// </summary>
public class ModeloPermissaoEstabelecimento : Entity
{
    /// <summary>
    /// NULL quando se trata do registro global de referência (escopo sistema).
    /// NOT NULL quando se trata de uma cópia pertencente a um estabelecimento.
    /// Nunca use em queries de tenant sem filtrar por estabelecimento_id = @X.
    /// </summary>
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual TipoAcessoModelo TipoAcesso { get; protected set; }

    /// <summary>JSONB persistido — array de strings com as keys de permissão (formato area.acao).</summary>
    public virtual string PermissoesJson { get; protected set; } = "[]";

    /// <summary>JSONB persistido — array de strings com permissões finas por feature (ver <see cref="PermissoesExtras"/>).</summary>
    public virtual string PermissoesExtrasJson { get; protected set; } = "[]";

    public virtual bool EhPadrao { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    /// <summary>Identificador FontAwesome (ex: <c>fa-user-doctor</c>) usado no front.</summary>
    public virtual string? Icone { get; protected set; }

    /// <summary>Cor (string HSL ou hex) usada para decorar o papel no front.</summary>
    public virtual string? Cor { get; protected set; }

    /// <summary>Descrição curta exibida no seletor de papel.</summary>
    public virtual string? Descricao { get; protected set; }

    /// <summary>Acessor tipado do array de permissões (deserializado de <see cref="PermissoesJson"/>).</summary>
    public IReadOnlyList<string> Permissoes =>
        JsonSerializer.Deserialize<List<string>>(PermissoesJson ?? "[]") ?? new();

    /// <summary>Acessor tipado das permissões finas (deserializado de <see cref="PermissoesExtrasJson"/>).</summary>
    public IReadOnlyList<string> PermissoesExtrasLista =>
        JsonSerializer.Deserialize<List<string>>(PermissoesExtrasJson ?? "[]") ?? new();

    protected ModeloPermissaoEstabelecimento() { }

    /// <summary>
    /// Modelos padrão criados automaticamente ao criar um estabelecimento — replicam
    /// o comportamento do legado (Admin / Médico / Recepção). Não podem ser editados
    /// nem excluídos.
    /// </summary>
    public static IReadOnlyList<ModeloPermissaoEstabelecimento> CriarPadroes(long estabelecimentoId)
    {
        var agora = DateTime.UtcNow;
        return new[]
        {
            new ModeloPermissaoEstabelecimento
            {
                EstabelecimentoId = (long?)estabelecimentoId,
                Nome = "Admin",
                TipoAcesso = TipoAcessoModelo.Profissional,
                PermissoesJson = SerializarLista(CatalogoPermissoes.AdminPadrao),
                PermissoesExtrasJson = SerializarLista(new[]
                {
                    PermissoesExtras.AssistenteClinicoIa,
                    PermissoesExtras.GerirPermissoes,
                    PermissoesExtras.ConfigEstabelecimento,
                    PermissoesExtras.GerirProfissionais,
                    PermissoesExtras.ModelosProntuario,
                    PermissoesExtras.AutomacaoConfig,
                }),
                Icone = "fa-crown",
                Cor = "hsl(280 60% 50%)",
                Descricao = "Acesso total — recomendado para o dono da clínica",
                EhPadrao = true,
                CriadoEm = agora,
            },
            new ModeloPermissaoEstabelecimento
            {
                EstabelecimentoId = (long?)estabelecimentoId,
                Nome = "Médico",
                TipoAcesso = TipoAcessoModelo.Profissional,
                PermissoesJson = SerializarLista(CatalogoPermissoes.MedicoPadrao),
                PermissoesExtrasJson = SerializarLista(new[]
                {
                    PermissoesExtras.AssistenteClinicoIa,
                    PermissoesExtras.ModelosProntuario,
                }),
                Icone = "fa-user-doctor",
                Cor = "hsl(254 56% 38%)",
                Descricao = "Profissional de saúde com agenda e prontuário",
                EhPadrao = true,
                CriadoEm = agora,
            },
            new ModeloPermissaoEstabelecimento
            {
                EstabelecimentoId = (long?)estabelecimentoId,
                Nome = "Recepção",
                TipoAcesso = TipoAcessoModelo.Recepcionista,
                PermissoesJson = SerializarLista(CatalogoPermissoes.RecepcaoPadrao),
                PermissoesExtrasJson = "[]",
                Icone = "fa-headset",
                Cor = "hsl(40 80% 50%)",
                Descricao = "Atendimento, agenda e cadastro de pacientes",
                EhPadrao = true,
                CriadoEm = agora,
            },
        };
    }

    /// <summary>
    /// Cria o registro global de referência (escopo sistema, <c>EstabelecimentoId = NULL</c>).
    /// Usado pelo Admin Global para criar/gerenciar os padrões do sistema.
    /// </summary>
    public static ModeloPermissaoEstabelecimento CriarGlobal(
        string nome,
        TipoAcessoModelo tipoAcesso,
        IReadOnlyList<string>? permissoes = null,
        IReadOnlyList<string>? permissoesExtras = null,
        string? icone = null,
        string? cor = null,
        string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");
        ValidarPermissoes(permissoes);
        ValidarPermissoesExtras(permissoesExtras);

        return new ModeloPermissaoEstabelecimento
        {
            EstabelecimentoId = null,
            Nome = nome.Trim(),
            TipoAcesso = tipoAcesso,
            PermissoesJson = SerializarLista(permissoes),
            PermissoesExtrasJson = SerializarLista(permissoesExtras),
            Icone = NormalizarTexto(icone, 50),
            Cor = NormalizarTexto(cor, 40),
            Descricao = NormalizarTexto(descricao, 200),
            EhPadrao = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Materializa uma cópia <c>eh_padrao=true</c> do registro global para o estabelecimento informado.
    /// Preserva <c>PermissoesExtrasJson</c> do global — cópias semeadas nascem com os extras do global.
    /// Usado pelo handler de propagação ao criar estabelecimento ou ao criar padrão retroativamente.
    /// </summary>
    public static ModeloPermissaoEstabelecimento CriarCopiaDeGlobal(
        ModeloPermissaoEstabelecimento global,
        long estabelecimentoId)
    {
        if (estabelecimentoId <= 0)
            throw new ArgumentException("estabelecimentoId inválido.", nameof(estabelecimentoId));
        if (global.EstabelecimentoId is not null)
            throw new ArgumentException("Origem deve ser um registro global (EstabelecimentoId null).", nameof(global));

        return new ModeloPermissaoEstabelecimento
        {
            EstabelecimentoId = (long?)estabelecimentoId,
            Nome = global.Nome,
            TipoAcesso = global.TipoAcesso,
            PermissoesJson = global.PermissoesJson,
            PermissoesExtrasJson = global.PermissoesExtrasJson,
            Icone = global.Icone,
            Cor = global.Cor,
            Descricao = global.Descricao,
            EhPadrao = true,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Sincroniza esta cópia com os dados do registro global correspondente.
    /// Usado exclusivamente pelo handler de propagação — não passa pelo guard <see cref="Atualizar"/>.
    /// Preserva <c>EstabelecimentoId</c> e <c>PermissoesExtrasJson</c> da cópia (R8 — extras não editadas pelo admin).
    /// </summary>
    public virtual void SincronizarComGlobal(
        string nome,
        TipoAcessoModelo tipoAcesso,
        string permissoesJson,
        string? icone,
        string? cor,
        string? descricao)
    {
        Nome = nome.Trim();
        TipoAcesso = tipoAcesso;
        PermissoesJson = permissoesJson;
        Icone = NormalizarTexto(icone, 50);
        Cor = NormalizarTexto(cor, 40);
        Descricao = NormalizarTexto(descricao, 200);
        AtualizadoEm = DateTime.UtcNow;
        // PermissoesExtrasJson permanece intacto (R8).
    }

    public static ModeloPermissaoEstabelecimento Criar(
        long estabelecimentoId,
        string nome,
        TipoAcessoModelo tipoAcesso,
        IReadOnlyList<string>? permissoes = null,
        IReadOnlyList<string>? permissoesExtras = null,
        string? icone = null,
        string? cor = null,
        string? descricao = null)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");
        ValidarPermissoes(permissoes);
        ValidarPermissoesExtras(permissoesExtras);

        return new ModeloPermissaoEstabelecimento
        {
            EstabelecimentoId = (long?)estabelecimentoId,
            Nome = nome.Trim(),
            TipoAcesso = tipoAcesso,
            PermissoesJson = SerializarLista(permissoes),
            PermissoesExtrasJson = SerializarLista(permissoesExtras),
            Icone = NormalizarTexto(icone, 50),
            Cor = NormalizarTexto(cor, 40),
            Descricao = NormalizarTexto(descricao, 200),
            EhPadrao = false,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Valida que toda permissão informada existe no catálogo. Sem isso, o front
    /// poderia mandar chaves arbitrárias ("permissao.fake") e o backend aceitava —
    /// risco de UI inconsistente e dificulta auditoria do que foi concedido.
    ///
    /// Aceita dois formatos (UsuarioTemAcao no repo trata os dois também):
    /// - Legado: chave da área (ex: "agenda", "pacientes") — concede TODAS as ações da área.
    /// - Granular: "area.acao" (ex: "agenda.ver") — exige a ação específica.
    /// </summary>
    private static void ValidarPermissoes(IReadOnlyList<string>? permissoes)
    {
        if (permissoes is null || permissoes.Count == 0) return;
        var areas = CatalogoPermissoes.Areas.Select(a => a.Chave);
        var validas = new HashSet<string>(
            CatalogoPermissoes.Todas.Concat(areas),
            StringComparer.Ordinal);
        var invalidas = permissoes
            .Select(p => p?.Trim())
            .Where(p => !string.IsNullOrEmpty(p) && !validas.Contains(p))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (invalidas.Count > 0)
            throw new BusinessException(
                $"Permissões inválidas: {string.Join(", ", invalidas)}. Use as chaves do catálogo.");
    }

    private static void ValidarPermissoesExtras(IReadOnlyList<string>? permissoesExtras)
    {
        if (permissoesExtras is null || permissoesExtras.Count == 0) return;
        var validas = PermissoesExtras.Todas;
        var invalidas = permissoesExtras
            .Where(p => !validas.Contains(p, StringComparer.Ordinal))
            .Distinct()
            .ToList();
        if (invalidas.Count > 0)
            throw new BusinessException(
                $"Permissões extras inválidas: {string.Join(", ", invalidas)}.");
    }

    public virtual void Atualizar(
        string nome,
        TipoAcessoModelo tipoAcesso,
        IReadOnlyList<string>? permissoes = null,
        IReadOnlyList<string>? permissoesExtras = null,
        string? icone = null,
        string? cor = null,
        string? descricao = null)
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser editado.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");
        ValidarPermissoes(permissoes);
        ValidarPermissoesExtras(permissoesExtras);

        Nome = nome.Trim();
        TipoAcesso = tipoAcesso;
        PermissoesJson = SerializarLista(permissoes);
        PermissoesExtrasJson = SerializarLista(permissoesExtras);
        Icone = NormalizarTexto(icone, 50);
        Cor = NormalizarTexto(cor, 40);
        Descricao = NormalizarTexto(descricao, 200);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Garante que apenas modelos não-padrão possam ser excluídos.</summary>
    public virtual void GarantirPodeExcluir()
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser excluído.");
    }

    /// <summary>
    /// Indica se este modelo concede acesso à área informada (em qualquer ação).
    /// Aceita tanto chave legada (<c>"agenda"</c>) quanto chave granular (<c>"agenda.view"</c>).
    /// </summary>
    public virtual bool TemArea(string area)
    {
        if (string.IsNullOrWhiteSpace(area)) return false;
        var trim = area.Trim();
        var prefixo = trim + ".";
        foreach (var p in Permissoes)
        {
            if (p.Equals(trim, StringComparison.Ordinal)) return true;
            if (p.StartsWith(prefixo, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>
    /// Indica se este modelo concede a ação granular informada (formato <c>"area.acao"</c>).
    /// Para modelos legados (sem ponto), considera que ter a área concede todas as ações dela.
    /// </summary>
    public virtual bool TemAcao(string area, string acao)
    {
        if (string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(acao)) return false;
        var areaTrim = area.Trim();
        var chaveCompleta = $"{areaTrim}.{acao.Trim()}";
        foreach (var p in Permissoes)
        {
            if (p.Equals(chaveCompleta, StringComparison.Ordinal)) return true;
            // Legacy: chave sem ponto = acesso total à área.
            if (p.Equals(areaTrim, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>Indica se este modelo concede a permissão fina informada (catálogo em <see cref="PermissoesExtras"/>).</summary>
    public virtual bool TemPermissaoExtra(string permissao)
    {
        if (string.IsNullOrWhiteSpace(permissao)) return false;
        return PermissoesExtrasLista.Contains(permissao.Trim(), StringComparer.Ordinal);
    }

    public virtual void AdicionarPermissaoExtra(string permissao)
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser editado.");
        if (string.IsNullOrWhiteSpace(permissao))
            throw new BusinessException("Permissão é obrigatória.");

        var lista = PermissoesExtrasLista.ToList();
        var trim = permissao.Trim();
        if (!lista.Contains(trim, StringComparer.Ordinal))
            lista.Add(trim);
        PermissoesExtrasJson = SerializarLista(lista);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void RemoverPermissaoExtra(string permissao)
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser editado.");
        if (string.IsNullOrWhiteSpace(permissao)) return;

        var lista = PermissoesExtrasLista.ToList();
        var trim = permissao.Trim();
        if (lista.RemoveAll(p => string.Equals(p, trim, StringComparison.Ordinal)) > 0)
        {
            PermissoesExtrasJson = SerializarLista(lista);
            AtualizadoEm = DateTime.UtcNow;
        }
    }

    private static string SerializarLista(IReadOnlyList<string>? itens)
    {
        if (itens is null || itens.Count == 0) return "[]";
        var visto = new HashSet<string>();
        var ordenadas = new List<string>(itens.Count);
        foreach (var p in itens)
        {
            var trim = p?.Trim();
            if (string.IsNullOrEmpty(trim)) continue;
            if (visto.Add(trim)) ordenadas.Add(trim);
        }
        return JsonSerializer.Serialize(ordenadas);
    }

    private static string? NormalizarTexto(string? valor, int tamanhoMax)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var trim = valor.Trim();
        return trim.Length > tamanhoMax ? trim[..tamanhoMax] : trim;
    }
}
