using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Salas;

/// <summary>
/// Repartição/sala de atendimento de uma unidade do estabelecimento.
/// Sempre pertence a uma <see cref="Unidades.UnidadeEstabelecimento"/> e opcionalmente
/// referencia um <see cref="TipoSala"/> system-wide (seed).
/// </summary>
public class Sala : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long UnidadeId { get; protected set; }
    public virtual long? TipoSalaId { get; protected set; }
    public virtual string Nome { get; protected set; }
    public virtual string Descricao { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected Sala() { }

    public static Sala Criar(
        long estabelecimentoId,
        long unidadeId,
        long? tipoSalaId,
        string nome,
        string descricao)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (unidadeId <= 0)
            throw new BusinessException("Selecione a unidade onde a repartição está localizada.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da repartição é obrigatório.");

        return new Sala
        {
            EstabelecimentoId = estabelecimentoId,
            UnidadeId = unidadeId,
            TipoSalaId = tipoSalaId,
            Nome = nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        };
    }

    public virtual void Desativar()
    {
        if (!Ativo)
            throw new BusinessException("Repartição já está inativa.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo)
            throw new BusinessException("Repartição já está ativa.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AtualizarDados(long unidadeId, long? tipoSalaId, string nome, string descricao)
    {
        if (unidadeId <= 0)
            throw new BusinessException("Selecione a unidade onde a repartição está localizada.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da repartição é obrigatório.");

        UnidadeId = unidadeId;
        TipoSalaId = tipoSalaId;
        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }
}
