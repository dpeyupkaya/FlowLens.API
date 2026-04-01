using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces.External;
using FlowLens.Application.Interfaces.Infrastructure;
using MediatR;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;

public class AnalyzeRepoCommandHandler : IRequestHandler<AnalyzeRepoCommand, AnalysisReportDto>
{
    private readonly IGitHubService _gitHubService;
    private readonly ICodeAnalyzerService _codeAnalyzerService;

    public AnalyzeRepoCommandHandler(IGitHubService gitHubService, ICodeAnalyzerService codeAnalyzerService)
    {
        _gitHubService = gitHubService;
        _codeAnalyzerService = codeAnalyzerService;
    }

    public async Task<AnalysisReportDto> Handle(AnalyzeRepoCommand request, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "FlowLens", Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(tempPath);

            await _gitHubService.DownloadAndExtractRepoAsync(request.RepoUrl, request.AccessToken, tempPath);

            var csharpFiles = Directory.GetFiles(tempPath, "*.cs", SearchOption.AllDirectories);
            int totalLines = 0;
            foreach (var file in csharpFiles)
            {
                var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                totalLines += lines.Length;
            }

            var codeGraph = await _codeAnalyzerService.AnalyzeStructureAsync(tempPath);

            return new AnalysisReportDto(
                RepoUrl: request.RepoUrl,
                TotalFilesScanned: csharpFiles.Length,
                TotalLinesOfCode: totalLines,
                Graph: codeGraph,
                Issues: new List<string>
                {
                    $"Haritalama bitti: {codeGraph.Nodes.Count} nesne (sınıf/metot) tespit edildi.",
                    $"Toplam {totalLines} satır kod analiz için hazır."
                }
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Analiz işlemi başarısız: {ex.Message}");
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }
    }
}