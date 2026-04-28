using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Template de prontuário. Existe em dois escopos:
/// <list type="bullet">
/// <item><b>Padrão-sistema</b>: <see cref="EstabelecimentoId"/> null + <see cref="EhPadraoSistema"/>
///   true — gerenciado pela ferramenta admin e disponível para todos os estabelecimentos.</item>
/// <item><b>Do estabelecimento</b>: <see cref="EstabelecimentoId"/> NOT NULL + <see cref="EhPadraoSistema"/>
///   false — criado pelo dono do estabelecimento para uso interno.</item>
/// </list>
/// O campo <see cref="EstruturaJson"/> contém um array de seções (JSONB) — cada item descreve
/// uma seção do prontuário (chave, título, tipo, ordem, etc.). Schema versionável.
/// </summary>
public class ModeloDeProntuario : Entity
{
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; }
    public virtual string Descricao { get; protected set; }
    public virtual string EstruturaJson { get; protected set; }
    public virtual bool EhPadraoSistema { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ModeloDeProntuario() { }

    /// <summary>Cria um modelo pertencente a um estabelecimento.</summary>
    public static ModeloDeProntuario CriarDoEstabelecimento(
        long estabelecimentoId,
        string nome,
        string descricao,
        string estruturaJson)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");

        Validar(nome, estruturaJson);

        return new ModeloDeProntuario
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Descricao = descricao?.Trim(),
            EstruturaJson = estruturaJson,
            EhPadraoSistema = false,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um modelo padrão-sistema (chamado pela ferramenta admin). Sem estabelecimento.
    /// </summary>
    public static ModeloDeProntuario CriarPadraoSistema(
        string nome,
        string descricao,
        string estruturaJson)
    {
        Validar(nome, estruturaJson);

        return new ModeloDeProntuario
        {
            EstabelecimentoId = null,
            Nome = nome.Trim(),
            Descricao = descricao?.Trim(),
            EstruturaJson = estruturaJson,
            EhPadraoSistema = true,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void AtualizarDados(string nome, string descricao, string estruturaJson)
    {
        if (!Ativo)
            throw new BusinessException("Modelo inativo não pode ser alterado.");

        Validar(nome, estruturaJson);

        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        EstruturaJson = estruturaJson;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Modelo já está inativo.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Modelo já está ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void Validar(string nome, string estruturaJson)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");
        if (string.IsNullOrWhiteSpace(estruturaJson))
            throw new BusinessException("Estrutura do modelo é obrigatória.");
    }
}
