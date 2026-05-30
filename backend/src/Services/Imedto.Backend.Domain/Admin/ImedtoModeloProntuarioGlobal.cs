using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Modelo de prontuário global gerenciado pelo admin do sistema.
/// Tabela global — sem estabelecimento_id. Tenants importam via cópia independente.
///
/// Nome da tabela no Postgres: <c>imedto_modelo_prontuario_global</c>.
/// Índices: unique LOWER(nome), (ativo, nome) para listagem.
/// </summary>
public class ImedtoModeloProntuarioGlobal : Entity<Guid>
{
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? Descricao { get; protected set; }

    /// <summary>
    /// Estrutura do modelo em JSON. Compatível com o campo <c>conteudo_json</c>
    /// da entidade tenant <c>ModeloDeProntuario</c> para que a importação seja direta.
    /// </summary>
    public virtual string ConteudoJson { get; protected set; } = "{}";

    public virtual bool Ativo { get; protected set; } = true;
    public virtual DateTimeOffset CriadoEm { get; protected set; }
    public virtual DateTimeOffset? AtualizadoEm { get; protected set; }
    public virtual Guid? CriadoPorAdminId { get; protected set; }
    public virtual Guid? AtualizadoPorAdminId { get; protected set; }

    protected ImedtoModeloProntuarioGlobal() { }

    public static ImedtoModeloProntuarioGlobal Criar(
        string nome,
        string? descricao,
        string conteudoJson,
        Guid? criadoPorAdminId)
    {
        ValidarNome(nome);
        ValidarConteudoJson(conteudoJson);

        return new ImedtoModeloProntuarioGlobal
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            Descricao = descricao?.Trim(),
            ConteudoJson = conteudoJson,
            Ativo = true,
            CriadoEm = DateTimeOffset.UtcNow,
            AtualizadoEm = DateTimeOffset.UtcNow,
            CriadoPorAdminId = criadoPorAdminId,
            AtualizadoPorAdminId = criadoPorAdminId
        };
    }

    public virtual void Atualizar(string nome, string? descricao, string conteudoJson, Guid? atualizadoPorAdminId)
    {
        ValidarNome(nome);
        ValidarConteudoJson(conteudoJson);
        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        ConteudoJson = conteudoJson;
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorAdminId = atualizadoPorAdminId;
    }

    public virtual void Desativar(Guid? atualizadoPorAdminId)
    {
        Ativo = false;
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorAdminId = atualizadoPorAdminId;
    }

    public virtual void Reativar(Guid? atualizadoPorAdminId)
    {
        Ativo = true;
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorAdminId = atualizadoPorAdminId;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");
        if (nome.Trim().Length > 200)
            throw new BusinessException("Nome do modelo não pode exceder 200 caracteres.");
    }

    private static void ValidarConteudoJson(string conteudoJson)
    {
        if (string.IsNullOrWhiteSpace(conteudoJson))
            throw new BusinessException("Conteúdo JSON é obrigatório.");
    }
}
