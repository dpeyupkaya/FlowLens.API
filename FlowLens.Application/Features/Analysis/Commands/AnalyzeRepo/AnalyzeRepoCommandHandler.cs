using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces.External;
using FlowLens.Application.Interfaces.Infrastructure;
using MediatR;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;

public class AnalyzeRepoCommandHandler : IRequestHandler<AnalyzeRepoCommand, AnalysisReportDto>
{
    private readonly IGitHubService _gitHubService;
    private readonly ICodeAnalyzerService _codeAnalyzerService;
    private readonly IAnalysisProgressService _progressService;

    public AnalyzeRepoCommandHandler(
        IGitHubService gitHubService,
        ICodeAnalyzerService codeAnalyzerService,
        IAnalysisProgressService progressService)
    {
        _gitHubService = gitHubService;
        _codeAnalyzerService = codeAnalyzerService;
        _progressService = progressService;
    }

    public async Task<AnalysisReportDto> Handle(AnalyzeRepoCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = Guid.NewGuid().ToString();
        var tempPath = Path.Combine(Path.GetTempPath(), "FlowLens", workspaceId);

        try
        {
            await _progressService.NotifyAsync(" Analiz motoru hazırlık süreci tamamlandı.");
            Directory.CreateDirectory(tempPath);

            await _progressService.NotifyAsync(" Kaynak kod deposu indirme ve dışa aktarma işlemi yürütülüyor.");
            await _gitHubService.DownloadAndExtractRepoAsync(request.RepoUrl, request.AccessToken, tempPath);

            await _progressService.NotifyAsync("Dosya meta verileri ve kod metrikleri hesaplanıyor.");

            var csharpFiles = Directory.GetFiles(tempPath, "*.cs", SearchOption.AllDirectories);
            int totalLines = 0;

            foreach (var file in csharpFiles)
            {
                var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                totalLines += lines.Length;
            }

            await _progressService.NotifyAsync($" Statik tarama sonucunda {csharpFiles.Length} dosya ve {totalLines} satır kod analiz kapsamına alındı.");

            var codeGraph = await _codeAnalyzerService.AnalyzeStructureAsync(tempPath);

            await _progressService.NotifyAsync(" Proje yapısal analizi ve haritalama işlemi başarıyla tamamlandı.");

            return new AnalysisReportDto(
                RepoUrl: request.RepoUrl,
                TotalFilesScanned: csharpFiles.Length,
                TotalLinesOfCode: totalLines,
                Graph: codeGraph,
                Issues: new List<string>
                {
                    "Analiz süreci yürütme hatası alınmadan tamamlanmıştır.",
                    $"Analiz kapsamı: {codeGraph.Nodes.Count} yapısal birim haritalandı."
                }
            );
        }
        catch (Exception ex)
        {
            await _progressService.NotifyAsync($"[HATA] Analiz süreci başarısız oldu: {ex.Message}");
           
            throw new Exception("İlgili deponun analiz işlemi gerçekleştirilemedi.", ex);
        }
        finally
        {
          
            if (Directory.Exists(tempPath))
            {
                await _progressService.NotifyAsync(" Geçici çalışma dizini ve ilgili kaynaklar temizleniyor.");
                Directory.Delete(tempPath, true);
            }
        }
    }
}