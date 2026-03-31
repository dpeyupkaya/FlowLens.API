using FlowLens.Domain.Entities;

namespace FlowLens.Domain.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByGitHubIdAsync(string gitHubId);
}