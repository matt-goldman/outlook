using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Zuemail.Core.Models;
using Zuemail.Core.Services;
using Zuemail.Core.Extensions;
using Zuemail.MailKit.Services;
using Zuemail.MailKit.Models;
using Zuemail.MailKit.Extensions;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole()); //.AddSimpleConsole(o => { o.IncludeScopes = true; o.TimestampFormat = "HH:mm:ss.f "; })
var logger = loggerFactory.CreateLogger<Program>();

var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json").Build();

var emailSenderOptions = configuration.GetRequiredSection(EmailOptions.SectionName).Get<SmtpOptions>()!;
using var smtpSender = new MailKitSmtpSender(Options.Create(emailSenderOptions), new MimeMessageValidator(),
    loggerFactory.CreateLogger<MailKitSmtpSender>());
await smtpSender.SmtpClient.GetConnectedAuthenticatedAsync<SmtpClient>(emailSenderOptions);
var writtenEmail = new EmailMessageBuilder()
    .Compose(email => email
        .From("my.name@example.com")
        .Subject("Hello World")
        .Body("Optional text/plain content.", "Optional text/html content.<br/>"))
    .Attach(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
    .SaveAsTemplate();
var firstEmail = writtenEmail.Copy().To("YourName@example.com").Build();
bool isSent = await smtpSender.TrySendAsync(firstEmail);
logger.LogInformation("Email 1 {result}.", isSent ? "sent" : "failed to send");

var secondEmail = writtenEmail.Copy().To("new@io").Build();
isSent = await smtpSender.TrySendAsync(secondEmail);
logger.LogInformation("Email 2 {result}.", isSent ? "sent" : "failed to send");

var emailReceiverOptions = configuration.GetRequiredSection(EmailOptions.SectionName).Get<ImapOptions>()!;
using var imapClient = new ImapClient { Timeout = emailReceiverOptions.TimeoutMs };
await imapClient.ConnectAsync(emailReceiverOptions.Host, emailReceiverOptions.Port, emailReceiverOptions.SocketOptions);
await imapClient.AuthenticateAsync(emailReceiverOptions.Credential);

var inbox = imapClient.Inbox;
await inbox.OpenAsync(FolderAccess.ReadOnly);
if (inbox.Count > 5)
{
    var messageSummaries = await inbox.FetchAsync(5, Math.Min(6, inbox.Count - 1), MessageSummaryItems.UniqueId);
    logger.LogDebug("Email(s) received: {ids}.", string.Join(", ", messageSummaries.Select(message => message.UniqueId)));
}
if (inbox.Count > 0)
{
    using var message = await inbox.GetMessageAsync(0);
    logger.LogDebug("Email received: {id}.", message.MessageId);
}

if (imapClient.Capabilities.HasFlag(ImapCapabilities.Idle))
{
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, args) => { args.Cancel = true; cancellation.Cancel(); };
    inbox.CountChanged += (_, _) => logger.LogInformation("Inbox contains {count} messages.", inbox.Count);
    try
    {
        while (!cancellation.IsCancellationRequested)
        {
            using var idle = new CancellationTokenSource(TimeSpan.FromMinutes(9));
            await imapClient.IdleAsync(idle.Token, cancellation.Token);
        }
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
    }
}
await imapClient.DisconnectAsync(true);
