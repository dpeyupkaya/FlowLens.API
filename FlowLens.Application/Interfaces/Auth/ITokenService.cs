using FlowLens.Domain.Entities;

namespace FlowLens.Application.Interfaces.Auth;

public interface ITokenService
{
    string CreateToken(User user);
}