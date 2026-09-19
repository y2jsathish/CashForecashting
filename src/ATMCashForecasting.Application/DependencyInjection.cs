using ATMCashForecasting.Application.Forecasting;
using ATMCashForecasting.Application.Replenishment;
using ATMCashForecasting.Application.Risk;
using ATMCashForecasting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ATMCashForecasting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IForecastingEngine, ForecastingEngine>();
        services.AddScoped<IRiskClassifier, RiskClassifier>();
        services.AddScoped<IReplenishmentEngine, ReplenishmentEngine>();

        services.AddScoped<IAtmService, AtmService>();
        services.AddScoped<ITransactionImportService, TransactionImportService>();
        services.AddScoped<IForecastService, ForecastService>();
        services.AddScoped<IReplenishmentService, ReplenishmentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
