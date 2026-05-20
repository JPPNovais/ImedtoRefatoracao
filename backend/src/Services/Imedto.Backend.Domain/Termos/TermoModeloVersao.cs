using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Snapshot imutável de uma versão do modelo (<see cref="TermoModelo"/>). Cada bump de
/// versão grava uma nova linha. Não há método de mutação — registros são append-only.
/// </summary>
public class TermoModeloVersao : Entity
{
    public virtual long TermoModeloId { get; protected set; }
    public virtual int Versao { get; protected set; }
    public virtual string ConteudoHtml { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual Guid? CriadoPorUsuarioId { get; protected set; }

    protected TermoModeloVersao() { }

    internal static TermoModeloVersao Registrar(long termoModeloId, int versao, string conteudoHtml, Guid? autorUsuarioId)
    {
        if (termoModeloId <= 0)
            throw new ArgumentOutOfRangeException(nameof(termoModeloId));
        if (versao <= 0)
            throw new ArgumentOutOfRangeException(nameof(versao));
        if (string.IsNullOrEmpty(conteudoHtml))
            throw new ArgumentException("Conteúdo HTML obrigatório.", nameof(conteudoHtml));

        return new TermoModeloVersao
        {
            TermoModeloId = termoModeloId,
            Versao = versao,
            ConteudoHtml = conteudoHtml,
            CriadoEm = DateTime.UtcNow,
            CriadoPorUsuarioId = autorUsuarioId,
        };
    }
}
