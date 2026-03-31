namespace FlowLens.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow; 
    public DateTimeOffset? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedDate { get; set; }
}