using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class OrcamentoPacoteProcedimento : Entity
{
    public virtual long PacoteId { get; protected set; }
    public virtual long CatalogoCirurgiaId { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected OrcamentoPacoteProcedimento() { }

    internal static OrcamentoPacoteProcedimento Criar(long catalogoCirurgiaId, int ordem)
    {
        if (catalogoCirurgiaId <= 0) throw new BusinessException("Procedimento é obrigatório.");
        return new OrcamentoPacoteProcedimento { CatalogoCirurgiaId = catalogoCirurgiaId, Ordem = ordem };
    }
}
