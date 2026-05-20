using FlowLens.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Infrastructure.Services
{
    public class DailyLimitResetWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyLimitResetWorker> _logger;

        public DailyLimitResetWorker(IServiceProvider serviceProvider, ILogger<DailyLimitResetWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Günlük limit sıfırlama arka plan servisi başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var tsiNow = DateTime.UtcNow.AddHours(3);

                var nextMidnight = tsiNow.Date.AddDays(1);

                var delay = nextMidnight - tsiNow;

                _logger.LogInformation($"Bir sonraki limit sıfırlama işlemi için {delay.Hours} saat {delay.Minutes} dakika bekleniyor...");

                await Task.Delay(delay, stoppingToken);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var limitService = scope.ServiceProvider.GetRequiredService<IAnalysisLimitService>();
                        await limitService.ResetAllDailyLimitsAsync(stoppingToken);
                    }

                    _logger.LogInformation($"[BAŞARILI] Tüm kullanıcıların günlük analiz hakları sıfırlandı. TSİ Saat: {DateTime.UtcNow.AddHours(3)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[HATA] Günlük limitler sıfırlanırken bir hata oluştu.");
                }
            }
        }
    }
}