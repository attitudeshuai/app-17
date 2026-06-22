using MedCabinet.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Infrastructure.Services;

public class BorrowOverdueCheckerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BorrowOverdueCheckerService> _logger;
    private readonly TimeSpan _checkInterval;

    public BorrowOverdueCheckerService(
        IServiceProvider serviceProvider,
        ILogger<BorrowOverdueCheckerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _checkInterval = TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("借用逾期检查后台服务已启动，检查间隔: {Interval}", _checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var borrowService = scope.ServiceProvider.GetRequiredService<IBorrowService>();

                _logger.LogInformation("开始执行借用逾期检查...");
                var result = await borrowService.CheckAndSendOverdueRemindersAsync();

                if (result.Code == 200)
                {
                    _logger.LogInformation("借用逾期检查完成: {Message}", result.Message);
                }
                else
                {
                    _logger.LogWarning("借用逾期检查返回异常: Code={Code}, Message={Message}", result.Code, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行借用逾期检查时发生未处理异常");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("借用逾期检查后台服务已停止");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("借用逾期检查后台服务正在停止...");
        await base.StopAsync(cancellationToken);
    }
}
