using System;
using Zuemail.Core.Abstractions;
using Zuemail.Core.Models;
using Microsoft.Extensions.Options;

namespace Zuemail.Core.Services
{
    public class EmailMessageBuilder : IEmailMessageBuilder
    {
        public EmailMessageBuilder(IOptions<IEmailMessage> options = null)
        {
            Template = options?.Value;
            Email = (Template ?? EmailMessage.Default).Copy();
        }

        public virtual IEmailMessage Email { get; protected set; }

        private IEmailMessage _template;

        public virtual IEmailMessage Template
        {
            get => _template;
            set => _template = value?.Copy();
        }

        public virtual IEmailMessageBuilder SaveAsTemplate()
        {
            Template = Email;
            return this;
        }

        /// <inheritdoc/>
        public virtual IEmailMessageBuilder Compose(Action<IEmailMessageBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            configure(this);
            return this;
        }

        /// <inheritdoc/>
        public virtual IEmailMessageBuilder Body(string plainText, string html = null)
        {
            Email.BodyText = plainText ?? string.Empty;
            Email.BodyHtml = html ?? string.Empty;
            return this;
        }

        /// <inheritdoc/>
        public virtual IEmailMessage Build() => Email.Copy();

        public virtual IEmailMessageBuilder Header(string key, string value)
        {
            Email.Headers.Add(key, value);
            return this;
        }

        public virtual IEmailMessageBuilder From(string emailAddress, string name = null)
        {
            Email.From.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public virtual IEmailMessageBuilder To(string emailAddress, string name = null)
        {
            Email.To.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public virtual IEmailMessageBuilder Cc(string emailAddress, string name = null)
        {
            Email.Cc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public virtual IEmailMessageBuilder Bcc(string emailAddress, string name = null)
        {
            Email.Bcc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public virtual IEmailMessageBuilder Subject(string subject)
        {
            Email.Subject = subject ?? string.Empty;
            return this;
        }

        public virtual IEmailMessageBuilder Subject(string prefix, string suffix)
        {
            Email.Subject = $"{prefix}{Email.Subject}{suffix}";
            return this;
        }

        public virtual IEmailMessageBuilder Attach(string key, object value = null)
        {
            Email.Attachments.Add(key, value);
            return this;
        }

        public virtual IEmailMessageBuilder BodyText(string plainText)
        {
            Email.BodyText = plainText ?? string.Empty;
            return this;
        }

        public virtual IEmailMessageBuilder BodyHtml(string htmlText)
        {
            Email.BodyHtml = htmlText ?? string.Empty;
            return this;
        }

        public virtual IEmailMessageBuilder Copy()
        {
            var copy = (EmailMessageBuilder)MemberwiseClone();
            copy.Email = Email.Copy();
            copy.Template = Template;
            return copy;
        }
    }
}
