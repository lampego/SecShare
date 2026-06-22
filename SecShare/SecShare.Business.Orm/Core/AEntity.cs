using Domain.Abstractions;

namespace SecShare.Business.Orm.Core;

public class AEntity : IEntity
{
    public virtual Guid Id { get; set; }

    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual DateTime? UpdatedAt { get; set; }
    public virtual DateTime? DeletedAt { get; set; }

    public virtual bool IsDeleted => DeletedAt != null;
    public virtual bool IsNew => Id == Guid.Empty;

    public override int GetHashCode()
    {
        return Id == Guid.Empty ? base.GetHashCode() : Id.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IHasId other)
        {
            return ReferenceEquals(this, obj);
        }

        if (Id == Guid.Empty && other.Id == Guid.Empty)
        {
            return ReferenceEquals(this, other);
        }

        return Id == other.Id;
    }

    public static bool operator ==(AEntity? left, AEntity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(AEntity? left, AEntity? right)
    {
        return !Equals(left, right);
    }
}
