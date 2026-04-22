using FlowLens.Domain.Entities;
using FlowLens.Domain.Repositories;
using MediatR;

namespace FlowLens.Application.Features.Users.Commands.UpdateUserSettings;

public class UpdateUserSettingsCommandHandler : IRequestHandler<UpdateUserSettingsCommand, bool>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserSettingsCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null)
            return false;
        user.Settings = new UserSettings
        {
            Analysis = new AnalysisPreferences
            {
                ExcludedFolders = request.Settings.Analysis.ExcludedFolders,
                MaxAnalysisDepth = request.Settings.Analysis.MaxAnalysisDepth,
                ShowExternalLibs = request.Settings.Analysis.ShowExternalLibs
            },
            Graphics = new GraphicsPreferences
            {
                NodeDetailLevel = request.Settings.Graphics.NodeDetailLevel,
                HighPerformanceMode = request.Settings.Graphics.HighPerformanceMode,
                ShowMinimap = request.Settings.Graphics.ShowMinimap
            },
            Data = new DataPreferences
            {
                RepoVisibility = request.Settings.Data.RepoVisibility
            }
        };

        await _userRepository.UpdateAsync(user);

        return true;
    }
}