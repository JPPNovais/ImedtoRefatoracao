using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Salas;

/// <summary>
/// Tipo de sala/repartição. Catálogo system-wide gerido por seed (não por usuário final).
/// </summary>
public class TipoSala : Entity
{
    public virtual string Nome { get; protected set; }
    public virtual string Descricao { get; protected set; }

    protected TipoSala() { }
}
