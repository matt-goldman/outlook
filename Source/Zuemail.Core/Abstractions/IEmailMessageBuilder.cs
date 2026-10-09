using System;

namespace Zuemail.Core.Abstractions
{
    /// <summary>
    /// Interface for writing fluent emails.
    /// </summary>
    public interface IEmailMessageBuilder
    {
        /// <summary>Applies reusable fluent configuration without sending.</summary>
        /// <param name="configure">The composition executed immediately.</param>
        /// <returns>This builder.</returns>
        IEmailMessageBuilder Compose(Action<IEmailMessageBuilder> configure);

        /// <summary>Sets plain text and an optional HTML alternative.</summary>
        /// <param name="plainText">The plain-text content.</param>
        /// <param name="html">The HTML content, or null to clear it.</param>
        /// <returns>This builder.</returns>
        IEmailMessageBuilder Body(string plainText, string html = null);

        /// <summary>Creates an independent message snapshot without I/O.</summary>
        /// <returns>A message independent of future builder changes; legacy streams remain borrowed.</returns>
        IEmailMessage Build();

        /// <summary>
        /// Save a copy of the current message as the default template for new messages.
        /// </summary>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder SaveAsTemplate();

        /// <summary>
        /// Add a custom header to the email, prefixed with "X-"
        /// (<see href="https://www.rfc-editor.org/rfc/rfc822#section-4.7.4"/>).
        /// </summary>
        /// <param name="key">Header key ("X-FieldName").</param>
        /// <param name="value">Header value.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder Header(string key, string value);

        /// <summary>
        /// Add a sender's details to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of sender.</param>
        /// <param name="name">Name of sender.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder From(string emailAddress, string name = null);

        /// <summary>
        /// Add a recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder To(string emailAddress, string name = null);

        /// <summary>
        /// Add a carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder Cc(string emailAddress, string name = null);

        /// <summary>
        /// Add a blind-carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder Bcc(string emailAddress, string name = null);

        /// <summary>
        /// Sets or overwrites the subject of the email.
        /// </summary>
        /// <param name="subject">Email subject.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder Subject(string subject);

        /// <summary>
        /// Quote the original subject of the email.
        /// </summary>
        /// <param name="prefix">Prepend to subject.</param>
        /// <param name="suffix">Append to subject.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder Subject(string prefix, string suffix);

        /// <summary>
        /// Add an object as an attachment to the email.
        /// Key could be file path if value is null, or
        /// Content-Id if value is a byte[] or stream.
        /// Key may be ignored if value is a MimeEntity.
        /// </summary>
        /// <param name="key">Attachment key (e.g. Content-Id).</param>
        /// <param name="value">Attachemnt object value (e.g. byte[]).</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder Attach(string key, object value = null);

        /// <summary>
        /// Add a plain-text body to the email.
        /// </summary>
        /// <param name="textPlain">Body content as text/plain.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder BodyText(string textPlain);

        /// <summary>
        /// Add a HTML-formatted body to the email.
        /// </summary>
        /// <param name="textHtml">Body content as text/html.</param>
        /// <returns><see cref="IEmailMessageBuilder"/> interface.</returns>
        IEmailMessageBuilder BodyHtml(string textHtml);

        /// <summary>
        /// Get the email built as an <see cref="IEmailMessage"/>.
        /// </summary>
        IEmailMessage Email { get; }

        /// <summary>
        /// Copy this email writer to re-use it as a template.
        /// </summary>
        /// <returns>A builder with independent message and template state.</returns>
        IEmailMessageBuilder Copy();

        /// <summary>
        /// Email envelope (from, to, subject etc.) in a readable format.
        /// </summary>
        /// <returns>Email summary in plain text.</returns>
        string ToString();
    }
}
