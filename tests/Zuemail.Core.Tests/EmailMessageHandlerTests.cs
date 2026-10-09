using Zuemail.Core.Abstractions;
using Zuemail.Core.Models;
using Zuemail.Core.Services;
using Xunit;

namespace Zuemail.Core.Tests;

public class EmailMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_DefaultValidationAndTemplate_ResetAfterSuccess()
    {
        var sender = new FakeSender();
        var template = new EmailMessage { Subject = "Default" };
        template.From.Add(EmailContact.Create("sender@example.com"));
        var handler = EmailMessageHandler.Create(sender, template);
        template.Subject = "Changed external template";
        handler.To("first@example.com").Subject("First");

        await handler.SendAsync(TestContext.Current.CancellationToken);
        Assert.Equal("First", Assert.Single(sender.Sent).Subject);
        Assert.Empty(handler.Email.To);
        Assert.Equal("Default", handler.Email.Subject);
        Assert.NotSame(handler.Template, handler.Email);
        Assert.NotSame(handler.Template!.From[0], handler.Email.From[0]);

        handler.To("second@example.com");
        Assert.True(await handler.TrySendAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, sender.Sent.Count);
        Assert.Equal("second@example.com", Assert.Single(sender.Sent[1].To).EmailAddress);
        Assert.Empty(handler.Email.To);
    }

    [Fact]
    public async Task SendAsync_InvalidEmail_DoesNotSendOrMutate()
    {
        var sender = new FakeSender();
        var handler = new EmailMessageHandler(sender);
        handler.Subject("Keep me");
        var original = handler.Email;

        Assert.False(await handler.TrySendAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.SendAsync(TestContext.Current.CancellationToken));

        Assert.Empty(sender.Sent);
        Assert.Same(original, handler.Email);
        Assert.Empty(handler.Email.From);
        Assert.Empty(handler.Email.ReplyTo);
        Assert.Equal("Keep me", handler.Email.Subject);
    }

    [Fact]
    public async Task TrySendAsync_FalseOrException_RetainsMessage()
    {
        var sender = new FakeSender { TryResult = false };
        var handler = ValidHandler(sender);
        var original = handler.Email;

        Assert.False(await handler.TrySendAsync(TestContext.Current.CancellationToken));
        Assert.Same(original, handler.Email);
        sender.ThrowOnSend = true;
        await Assert.ThrowsAsync<IOException>(() => handler.SendAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => handler.TrySendAsync(TestContext.Current.CancellationToken));
        Assert.Same(original, handler.Email);
    }

    [Fact]
    public async Task SendAsync_PreCancelled_DoesNotSendOrReset()
    {
        var sender = new FakeSender();
        var handler = ValidHandler(sender);
        var original = handler.Email;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.SendAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.TrySendAsync(cancellation.Token));
        Assert.Empty(sender.Sent);
        Assert.Same(original, handler.Email);
    }

    [Fact]
    public async Task SendAsync_TransportCancellation_RetainsMessageAndPassesToken()
    {
        using var cancellation = new CancellationTokenSource();
        var sender = new FakeSender { CancelOnSend = cancellation };
        var handler = ValidHandler(sender);
        var original = handler.Email;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.SendAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, sender.LastToken);
        Assert.Same(original, handler.Email);
    }

    [Fact]
    public async Task SendAsync_CustomLegacyValidationFalse_IsRespected()
    {
        var sender = new FakeSender();
        var handler = ValidHandler(sender).SetStrategy(new LegacyValidation(false));

        Assert.False(await handler.TrySendAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.SendAsync(TestContext.Current.CancellationToken));
        Assert.Empty(sender.Sent);
        Assert.Throws<ArgumentNullException>(() => handler.SetStrategy(null!));
    }

    [Fact]
    public async Task SendAsync_CancellableValidation_ReceivesTokenAndStopsTransport()
    {
        using var cancellation = new CancellationTokenSource();
        var sender = new FakeSender();
        var validation = new CancellingValidation(cancellation);
        var handler = ValidHandler(sender).SetStrategy(validation);
        var original = handler.Email;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.SendAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, validation.LastToken);
        Assert.Empty(sender.Sent);
        Assert.Same(original, handler.Email);
    }

    [Fact]
    public async Task SendAsync_NoTemplate_ResetsToIndependentDefault()
    {
        var sender = new FakeSender();
        var handler = ValidHandler(sender);

        await handler.SendAsync(TestContext.Current.CancellationToken);
        handler.To("new@example.com");

        Assert.Empty(EmailMessage.Default.To);
        Assert.NotSame(EmailMessage.Default, handler.Email);
    }

    private static EmailMessageHandler ValidHandler(FakeSender sender)
    {
        var handler = new EmailMessageHandler(sender);
        handler.From("sender@example.com").To("recipient@example.com").Subject("Keep me");
        return handler;
    }

    private sealed class LegacyValidation(bool result) : IValidationStrategy<IEmailMessage>
    {
        public Task<bool> ValidateAsync(IEmailMessage item) => Task.FromResult(result);
    }

    private sealed class CancellingValidation(CancellationTokenSource cancellation) : ICancellableValidationStrategy<IEmailMessage>
    {
        public CancellationToken LastToken { get; private set; }
        public Task<bool> ValidateAsync(IEmailMessage item) => throw new InvalidOperationException("Use the cancellable overload.");
        public Task<bool> ValidateAsync(IEmailMessage item, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            cancellation.Cancel();
            return Task.FromResult(true);
        }
    }

    private sealed class FakeSender : IEmailSender<IEmailMessage>
    {
        public List<IEmailMessage> Sent { get; } = new();
        public bool TryResult { get; set; } = true;
        public bool ThrowOnSend { get; set; }
        public CancellationTokenSource? CancelOnSend { get; set; }
        public CancellationToken LastToken { get; private set; }
        public IEmailMessageHandler WriteEmail => new EmailMessageHandler(this);
        public IEmailMessage Copy() => new EmailMessage();
        public void Dispose() { }

        public Task SendAsync(IEmailMessage item, CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;
            CancelOnSend?.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowOnSend)
                throw new IOException("Expected send failure.");
            Sent.Add(item.Copy());
            return Task.CompletedTask;
        }

        public async Task<bool> TrySendAsync(IEmailMessage item, CancellationToken cancellationToken = default)
        {
            if (!TryResult && !ThrowOnSend)
                return false;
            await SendAsync(item, cancellationToken);
            return true;
        }
    }
}