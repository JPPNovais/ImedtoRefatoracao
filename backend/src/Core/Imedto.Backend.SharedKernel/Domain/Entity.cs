using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.SharedKernel.Domain;

/// <summary>
/// Classe base para entidades de domínio com tipo de ID parametrizável.
/// Use <see cref="Entity{TId}"/> quando o ID for UUID (<c>Guid</c>) ou outro tipo não-inteiro.
/// </summary>
public abstract class Entity<TId> : IDomainEventCarrier
{
    public virtual TId Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id is null ? 0 : Id.GetHashCode();
}

/// <summary>
/// Atalho para entidades com ID <c>long</c> (padrão de tabelas de domínio interno).
/// </summary>
public abstract class Entity : Entity<long>
{
}
