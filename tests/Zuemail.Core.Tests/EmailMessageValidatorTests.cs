using Zuemail.Core.Models;
using Zuemail.Core.Services;
using Xunit;

namespace Zuemail.Core.Tests;

public class EmailMessageValidatorTests
{
    [Theory]
    [InlineData("To")]
    [InlineData("Cc")]
    [InlineData("Bcc")]
    public async Task ValidateAsync_AcceptsAnyRecipientCollectionWithoutMutation(string collection)
    {
        var email = new EmailMessage();
        email.From.Add(EmailContact.Create("sender@example.com"));
        var contacts = collection switch { "Cc" => email.Cc, "Bcc" => email.Bcc, _ => email.To };
        contacts.Add(EmailContact.Create("sender@example.com"));

        Assert.True(await new EmailMessageValidator().ValidateAsync(email, TestContext.Current.CancellationToken));
        Assert.Single(email.From);
        Assert.Single(contacts);
        Assert.Empty(email.ReplyTo);
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("name")]
    [InlineData("address")]
    [InlineData("header-name")]
    [InlineData("header-value")]
    [InlineData("reply-address")]
    public async Task ValidateAsync_RejectsHeaderInjection(string field)
    {
        var email = new EmailMessage();
        email.From.Add(EmailContact.Create("sender@example.com"));
        email.To.Add(EmailContact.Create("recipient@example.com"));
        const string injection = "value\r\nBcc: victim@example.com";
        switch (field)
        {
            case "subject": email.Subject = injection; break;
            case "name": email.From[0].Name = injection; break;
            case "address": email.To[0].EmailAddress = injection; break;
            case "header-name": email.Headers[injection] = "value"; break;
            case "header-value": email.Headers["X-Test"] = injection; break;
            case "reply-address": email.ReplyTo.Add(new EmailContact { EmailAddress = injection }); break;
        }

        Assert.False(await new EmailMessageValidator().ValidateAsync(email, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ValidateAsync_InvalidOrMissingAddresses_ReturnsFalseWithoutInventingContacts()
    {
        var validator = new EmailMessageValidator();
        var email = new EmailMessage();
        Assert.False(await validator.ValidateAsync(email, TestContext.Current.CancellationToken));
        Assert.False(await validator.ValidateAsync(null!, TestContext.Current.CancellationToken));
        Assert.Empty(email.From);
        Assert.Empty(email.ReplyTo);

        email.From.Add(EmailContact.Create("sender@example.com"));
        email.To.Add(new EmailContact { EmailAddress = "not-an-address" });
        Assert.False(await validator.ValidateAsync(email, TestContext.Current.CancellationToken));
    }
}