using FlowLens.Domain.Entities;
using FlowLens.Domain.Repositories;
using FlowLens.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FlowLens.Persistence.Repositories;

public class UserRepository : GenericRepository<User, AppDbContext>, IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<User?> GetByGitHubIdAsync(string gitHubId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.GitHubId == gitHubId);
    }
}