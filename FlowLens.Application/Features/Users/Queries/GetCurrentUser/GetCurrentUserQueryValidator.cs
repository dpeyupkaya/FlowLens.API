using FluentValidation;

namespace FlowLens.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı kimliği doğrulanamadı. Lütfen tekrar giriş yapın.");
    }
}