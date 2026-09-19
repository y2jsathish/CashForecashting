using System.Net.Http.Json;
using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ATMCashForecasting.Infrastructure.Notifications;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "atm-alerts@bank.example";

    public string SmsGatewayUrl { get; set; } = string.Empty;
    public string SmsApiKey { get; set; } = string.Empty;

    public string TeamsWebhookUrl { get; set; } = string.Empty;
}

/// <summary>
/// SMTP-backed email sender. Wired via IOptions&lt;NotificationOptions&gt; so credentials come from
/// Azure Key Vault / appsettings in production, never hard-coded.
/// </summary>
public class EmailNotificationSender : INotificationSender
{
    private readonly NotificationOptions _options;
    private readonly ILogger<EmailNotificationSender> _logger;

    public NotificationChannel Channel => NotificationChannel.Email;

    public EmailNotificationSender(IOptions<NotificationOptions> options, ILogger<EmailNotificationSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            using var client = new System.Net.Mail.SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                Credentials = new System.Net.NetworkCredential(_options.SmtpUser, _options.SmtpPassword),
                EnableSsl = true
            };

            using var message = new System.Net.Mail.MailMessage(_options.FromAddress, recipient, subject, body);
            await client.SendMailAsync(message, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email alert to {Recipient}", recipient);
            return false;
        }
    }
}

/// <summary>SMS sender using a generic REST gateway (Twilio-compatible). Reserved for Critical alerts.</summary>
public class SmsNotificationSender : INotificationSender
{
    private readonly NotificationOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsNotificationSender> _logger;

    public NotificationChannel Channel => NotificationChannel.Sms;

    public SmsNotificationSender(IOptions<NotificationOptions> options, IHttpClientFactory httpClientFactory, ILogger<SmsNotificationSender> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmsGatewayUrl))
        {
            _logger.LogWarning("SMS gateway not configured; skipping SMS to {Recipient}", recipient);
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("SmsGateway");
            var response = await client.PostAsJsonAsync(_options.SmsGatewayUrl, new { to = recipient, message = body }, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS alert to {Recipient}", recipient);
            return false;
        }
    }
}

/// <summary>Posts an adaptive-card-style message to a Microsoft Teams incoming webhook.</summary>
public class TeamsNotificationSender : INotificationSender
{
    private readonly NotificationOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeamsNotificationSender> _logger;

    public NotificationChannel Channel => NotificationChannel.Teams;

    public TeamsNotificationSender(IOptions<NotificationOptions> options, IHttpClientFactory httpClientFactory, ILogger<TeamsNotificationSender> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.TeamsWebhookUrl))
        {
            _logger.LogWarning("Teams webhook not configured; skipping Teams notification");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("TeamsWebhook");
            var payload = new { text = $"**{subject}**\n\n{body}" };
            var response = await client.PostAsJsonAsync(_options.TeamsWebhookUrl, payload, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post Teams notification");
            return false;
        }
    }
}
