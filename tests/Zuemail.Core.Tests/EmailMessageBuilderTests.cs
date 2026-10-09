using Microsoft.Extensions.Options;
using Zuemail.Core.Abstractions;
using Zuemail.Core.Models;
using Zuemail.Core.Services;
using Xunit;

namespace Zuemail.Core.Tests;

public class EmailMessageBuilderTests
{
    [Fact]
    public void NewBuilders_HaveIndependentDefaultState()
    {
        var originalDefault = EmailMessage.Default.Copy();
        var first = new EmailMessageBuilder();
        var second = new EmailMessageBuilder();

        first.To("first@example.com").Header("X-Test-Isolation", "first");

        Assert.NotSame(first.Email, second.Email);
        Assert.Equal(originalDefault.To.Count, second.Email.To.Count);
        Assert.Equal(originalDefault.Headers.Count, second.Email.Headers.Count);
        Assert.Equal(originalDefault.To.Count, EmailMessage.Default.To.Count);
        Assert.Equal(originalDefault.Headers.Count, EmailMessage.Default.Headers.Count);
    }

    [Theory]
    [InlineData("From")]
    [InlineData("ReplyTo")]
    [InlineData("To")]
    [InlineData("Cc")]
    [InlineData("Bcc")]
    public void Copy_ContactsAndCollections_AreIndependent(string name)
    {
        var original = new EmailMessage();
        var contacts = GetContacts(original, name);
        contacts.Add(new EmailContact { EmailAddress = "original@example.com", Name = null });

        var copy = original.Copy();
        contacts[0].EmailAddress = "changed@example.com";
        contacts.Clear();

        var copied = Assert.Single(GetContacts(copy, name));
        Assert.Equal("original@example.com", copied.EmailAddress);
        Assert.Null(copied.Name);
    }

    [Fact]
    public void Copy_HeadersAndBytes_AreIndependentAndPreserveComparers()
    {
        var bytes = new byte[] { 1, 2 };
        var original = new EmailMessage
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "X-Test", "original" } },
            Attachments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "NOTE.BIN", bytes } }
        };

        var copy = original.Copy();
        original.Headers["X-Test"] = "changed";
        bytes[0] = 9;
        original.Attachments.Clear();

        Assert.Equal("original", copy.Headers["x-test"]);
        Assert.Equal(new byte[] { 1, 2 }, Assert.IsType<byte[]>(copy.Attachments["note.bin"]));
    }

    [Fact]
    public void Compose_ReusableConfiguration_BuildsIndependentMessages()
    {
        Action<IEmailMessageBuilder> defaults = email => email
            .From("sender@example.com")
            .Header("X-Campaign", "welcome")
            .Body("Welcome", "<p>Welcome</p>");
        var template = new EmailMessageBuilder().Compose(defaults);

        var first = template.Copy().To("first@example.com").Subject("First").Build();
        var second = template.Copy().To("second@example.com").Subject("Second").Build();
        first.Headers["X-Campaign"] = "changed";

        Assert.Equal("first@example.com", Assert.Single(first.To).EmailAddress);
        Assert.Equal("second@example.com", Assert.Single(second.To).EmailAddress);
        Assert.Equal("welcome", second.Headers["X-Campaign"]);
        Assert.Equal("Welcome", second.BodyText);
        Assert.Equal("<p>Welcome</p>", second.BodyHtml);
        Assert.Empty(template.Email.To);
    }

    [Fact]
    public void Build_DoesNotMutatePreviewOrAccessLegacyAttachments()
    {
        using var stream = new MemoryStream(new byte[] { 1 });
        var builder = new EmailMessageBuilder();
        builder.BodyHtml("<p>HTML</p>").Attach("stream.bin", stream).Attach("missing/path.txt");

        var snapshot = builder.Build();
        builder.Subject("Changed").To("new@example.com");

        Assert.Equal(string.Empty, builder.Email.BodyText);
        Assert.Equal(string.Empty, snapshot.BodyText);
        Assert.Equal(string.Empty, snapshot.Subject);
        Assert.Empty(snapshot.To);
        Assert.Same(stream, snapshot.Attachments["stream.bin"]);
        Assert.Null(snapshot.Attachments["missing/path.txt"]);
        Assert.Equal(0L, stream.Position);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void SaveAsTemplate_AndCopy_HaveIndependentState()
    {
        var builder = new EmailMessageBuilder();
        builder.To("original@example.com").Attach("bytes.bin", new byte[] { 1 }).SaveAsTemplate();
        var copy = Assert.IsType<EmailMessageBuilder>(builder.Copy());

        builder.Email.To[0].Name = "Changed live message";
        copy.Template!.To[0].EmailAddress = "changed-template@example.com";
        Assert.IsType<byte[]>(copy.Template.Attachments["bytes.bin"])[0] = 9;

        Assert.Equal("original@example.com", Assert.Single(builder.Template!.To).EmailAddress);
        Assert.NotEqual("Changed live message", builder.Template.To[0].Name);
        Assert.Equal(new byte[] { 1 }, Assert.IsType<byte[]>(builder.Template.Attachments["bytes.bin"]));
        Assert.Equal("original@example.com", Assert.Single(copy.Email.To).EmailAddress);
    }

    [Fact]
    public void ConfiguredTemplate_IsClonedForBuilderAndSavedDefaults()
    {
        IEmailMessage configured = new EmailMessage { Subject = "Default" };
        configured.From.Add(EmailContact.Create("sender@example.com"));
        var builder = new EmailMessageBuilder(Options.Create(configured));
        configured.Subject = "Changed configuration";

        Assert.Equal("Default", builder.Email.Subject);
        Assert.Equal("Default", builder.Template!.Subject);
        builder.Subject("Changed message");
        Assert.Equal("Default", builder.Template.Subject);
        Assert.NotSame(builder.Template.From[0], builder.Email.From[0]);
    }

    [Fact]
    public void Body_NullHtml_ClearsPreviousAlternative()
    {
        var builder = new EmailMessageBuilder();
        builder.Body("Original", "<p>Original</p>").Body("Plain only");

        Assert.Equal("Plain only", builder.Email.BodyText);
        Assert.Equal(string.Empty, builder.Email.BodyHtml);
        Assert.Throws<ArgumentNullException>(() => builder.Compose(null!));
    }

    private static IList<IEmailContact> GetContacts(IEmailMessage email, string name) => name switch
    {
        "From" => email.From,
        "ReplyTo" => email.ReplyTo,
        "To" => email.To,
        "Cc" => email.Cc,
        "Bcc" => email.Bcc,
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };
}