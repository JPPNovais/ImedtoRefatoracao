using System.Text.Json;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.ModelosPermissao;

/// <summary>
/// Template de permissões atribuído a profissionais de um estabelecimento.
/// <see cref="TipoAcesso"/> mantém o papel base (Profissional ou Recepcionista) — usado
/// por filtros de tenancy. <see cref="Permissoes"/> guarda as permissões granulares por
/// área (agenda, pacientes, prontuário, etc.) seguindo o catálogo do legado.
/// <see cref="PermissoesExtrasLista"/> guarda permissões finas por feature (catálogo em
/// <see cref="PermissoesExtras"/>) — ex: assistente clínico de IA, emissão de receitas.
/// </summary>
public class ModeloPermissaoEstabelecimento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual TipoAcessoModelo TipoAcesso { get; protected set; }

    /// <summary>JSONB persistido — array de strings com as keys de permissão (ver legado: src/constants/permissions.ts).</summary>
    public virtual string PermissoesJson { get; protected set; } = "[]";

    /// <summary>JSONB persistido — array de strings com permissões finas por feature (ver <see cref="PermissoesExtras"/>).</summary>
    public virtual string PermissoesExtrasJson { get; protected set; } = "[]";

    public virtual bool EhPadrao { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

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
                EstabelecimentoId = estabelecimentoId,
                Nome = "Admin",
                TipoAcesso = TipoAcessoModelo.Profissional,
                // Áreas: tudo que Admin enxerga no menu (paridade com seed legado).
                PermissoesJson = SerializarLista(new[]
                {
                    "agenda", "pacientes", "prontuario",
                    "orcamentos", "estoque", "financeiro", "relatorios",
                }),
                // Admin é o operador da clínica — recebe todas as permissões finas.
                PermissoesExtrasJson = SerializarLista(new[]
                {
                    PermissoesExtras.AssistenteClinicoIa,
                    PermissoesExtras.GerirPermissoes,
                    PermissoesExtras.ConfigEstabelecimento,
                    PermissoesExtras.GerirProfissionais,
                    PermissoesExtras.ModelosProntuario,
                    PermissoesExtras.AutomacaoConfig,
                }),
                EhPadrao = true,
                CriadoEm = agora,
            },
            new ModeloPermissaoEstabelecimento
            {
                EstabelecimentoId = estabelecimentoId,
                Nome = "Médico",
                TipoAcesso = TipoAcessoModelo.Profissional,
                // Áreas legado: agenda, pacientes, prontuario, perfil_profissional, minhas_consultas.
                PermissoesJson = SerializarLista(new[]
                {
                    "agenda", "pacientes", "prontuario",
                    "perfil_profissional", "minhas_consultas",
                }),
                // Finas: IA clínica + edição dos próprios templates de prontuário.
                PermissoesExtrasJson = SerializarLista(new[]
                {
                    PermissoesExtras.AssistenteClinicoIa,
                    PermissoesExtras.ModelosProntuario,
                }),
                EhPadrao = true,
                CriadoEm = agora,
            },
            new ModeloPermissaoEstabelecimento
            {
                EstabelecimentoId = estabelecimentoId,
                Nome = "Recepção",
                TipoAcesso = TipoAcessoModelo.Recepcionista,
                PermissoesJson = SerializarLista(new[] { "agenda", "pacientes", "estoque" }),
                // Recepção não tem acesso clínico nem administrativo — sem permissões finas.
                PermissoesExtrasJson = "[]",
                EhPadrao = true,
                CriadoEm = agora,
            },
        };
    }

    public static ModeloPermissaoEstabelecimento Criar(
        long estabelecimentoId,
        string nome,
        TipoAcessoModelo tipoAcesso,
        IReadOnlyList<string>? permissoes = null,
        IReadOnlyList<string>? permissoesExtras = null)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");

        return new ModeloPermissaoEstabelecimento
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            TipoAcesso = tipoAcesso,
            PermissoesJson = SerializarLista(permissoes),
            PermissoesExtrasJson = SerializarLista(permissoesExtras),
            EhPadrao = false,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        string nome,
        TipoAcessoModelo tipoAcesso,
        IReadOnlyList<string>? permissoes = null,
        IReadOnlyList<string>? permissoesExtras = null)
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser editado.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");

        Nome = nome.Trim();
        TipoAcesso = tipoAcesso;
        PermissoesJson = SerializarLista(permissoes);
        PermissoesExtrasJson = SerializarLista(permissoesExtras);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Garante que apenas modelos não-padrão possam ser excluídos.</summary>
    public virtual void GarantirPodeExcluir()
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser excluído.");
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
        // Normaliza: remove duplicatas, mantém a ordem original.
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
}
