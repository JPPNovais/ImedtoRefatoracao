using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Entrada de evolução em um prontuário. <b>Imutável após criação</b> — não possui
/// métodos de alteração. Para corrigir informação, cria-se nova evolução.
///
/// Guarda <see cref="ModeloSnapshotJson"/> — o schema do modelo no momento da criação —
/// para que a renderização futura respeite o template original, mesmo que o dono troque
/// o modelo ativo depois.
/// </summary>
public class ProntuarioEvolucao : Entity, ISoftDeletable
{
    public virtual long ProntuarioId { get; protected set; }
    public virtual Guid AutorUsuarioId { get; protected set; }
    public virtual string ConteudoJson { get; protected set; }
    public virtual string ModeloSnapshotJson { get; protected set; }
    public virtual long ModeloDeProntuarioIdOrigem { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }

    // Soft delete — LGPD: evoluções clínicas são append-only para fim de auditoria/legais.
    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    protected ProntuarioEvolucao() { }

    public static ProntuarioEvolucao Registrar(
        long prontuarioId,
        Guid autorUsuarioId,
        long modeloDeProntuarioIdOrigem,
        string modeloSnapshotJson,
        string conteudoJson)
    {
        if (prontuarioId <= 0)
            throw new BusinessException("Prontuário é obrigatório.");
        if (autorUsuarioId == Guid.Empty)
            throw new BusinessException("Autor é obrigatório.");
        if (modeloDeProntuarioIdOrigem <= 0)
            throw new BusinessException("Modelo de origem é obrigatório.");
        if (string.IsNullOrWhiteSpace(modeloSnapshotJson))
            throw new BusinessException("Snapshot do modelo é obrigatório.");
        if (string.IsNullOrWhiteSpace(conteudoJson))
            throw new BusinessException("Conteúdo da evolução é obrigatório.");

        return new ProntuarioEvolucao
        {
            ProntuarioId = prontuarioId,
            AutorUsuarioId = autorUsuarioId,
            ModeloDeProntuarioIdOrigem = modeloDeProntuarioIdOrigem,
            ModeloSnapshotJson = modeloSnapshotJson,
            ConteudoJson = conteudoJson,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void MarcarComoRegistrada()
    {
        if (Id == 0)
            throw new InvalidOperationException("Evolução ainda não foi persistida — Id é 0.");
        AddDomainEvent(new EvolucaoRegistradaEvent(Id, ProntuarioId, AutorUsuarioId));
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Evolução já está deletada.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }
}
