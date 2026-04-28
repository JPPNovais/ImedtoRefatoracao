using System.Text.Json;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.ModelosPermissao;

/// <summary>
/// Template de permissões atribuído a profissionais de um estabelecimento.
/// <see cref="TipoAcesso"/> mantém o papel base (Profissional ou Recepcionista) — usado
/// por filtros de tenancy. <see cref="Permissoes"/> guarda as permissões granulares por
/// área (agenda, pacientes, prontuário, etc.) seguindo o catálogo do legado.
/// </summary>
public class ModeloPermissaoEstabelecimento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual TipoAcessoModelo TipoAcesso { get; protected set; }

    /// <summary>JSONB persistido — array de strings com as keys de permissão (ver legado: src/constants/permissions.ts).</summary>
    public virtual string PermissoesJson { get; protected set; } = "[]";

    public virtual bool EhPadrao { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    /// <summary>Acessor tipado do array de permissões (deserializado de <see cref="PermissoesJson"/>).</summary>
    public IReadOnlyList<string> Permissoes =>
        JsonSerializer.Deserialize<List<string>>(PermissoesJson ?? "[]") ?? new();

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
                PermissoesJson = SerializarPermissoes(new[]
                {
                    "agenda", "pacientes", "prontuario", "profissionais", "permissoes",
                    "orcamentos", "estoque", "financeiro", "config_estabelecimento",
                    "relatorios", "automacao",
                }),
                EhPadrao = true,
                CriadoEm = agora,
            },
            new ModeloPermissaoEstabelecimento
            {
                EstabelecimentoId = estabelecimentoId,
                Nome = "Médico",
                TipoAcesso = TipoAcessoModelo.Profissional,
                PermissoesJson = SerializarPermissoes(new[] { "agenda", "pacientes", "prontuario" }),
                EhPadrao = true,
                CriadoEm = agora,
            },
            new ModeloPermissaoEstabelecimento
            {
                EstabelecimentoId = estabelecimentoId,
                Nome = "Recepção",
                TipoAcesso = TipoAcessoModelo.Recepcionista,
                PermissoesJson = SerializarPermissoes(new[] { "agenda", "pacientes", "estoque" }),
                EhPadrao = true,
                CriadoEm = agora,
            },
        };
    }

    public static ModeloPermissaoEstabelecimento Criar(
        long estabelecimentoId,
        string nome,
        TipoAcessoModelo tipoAcesso,
        IReadOnlyList<string>? permissoes = null)
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
            PermissoesJson = SerializarPermissoes(permissoes),
            EhPadrao = false,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, TipoAcessoModelo tipoAcesso, IReadOnlyList<string>? permissoes = null)
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser editado.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");

        Nome = nome.Trim();
        TipoAcesso = tipoAcesso;
        PermissoesJson = SerializarPermissoes(permissoes);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Garante que apenas modelos não-padrão possam ser excluídos.</summary>
    public virtual void GarantirPodeExcluir()
    {
        if (EhPadrao)
            throw new BusinessException("Modelo padrão do sistema não pode ser excluído.");
    }

    private static string SerializarPermissoes(IReadOnlyList<string>? permissoes)
    {
        if (permissoes is null || permissoes.Count == 0) return "[]";
        // Normaliza: remove duplicatas, mantém a ordem original.
        var visto = new HashSet<string>();
        var ordenadas = new List<string>(permissoes.Count);
        foreach (var p in permissoes)
        {
            var trim = p?.Trim();
            if (string.IsNullOrEmpty(trim)) continue;
            if (visto.Add(trim)) ordenadas.Add(trim);
        }
        return JsonSerializer.Serialize(ordenadas);
    }
}
