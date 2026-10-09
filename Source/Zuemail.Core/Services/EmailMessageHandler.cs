using System;
using System.Threading;
using System.Threading.Tasks;
using Zuemail.Core.Abstractions;
using Zuemail.Core.Models;
using Microsoft.Extensions.Options;

namespace Zuemail.Core.Services
{
    public sealed class EmailMessageHandler : EmailMessageBuilder, IEmailMessageHandler
    {
        private IValidationStrategy<IEmailMessage> _validationStrategy;
        private readonly IEmailSender<IEmailMessage> _emailSender;

        public EmailMessageHandler(IEmailSender<IEmailMessage> emailSender, IOptions<IEmailMessage> options = null, IValidationStrategy<IEmailMessage> validationStrategy = null) : base(options)
        {
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _validationStrategy = validationStrategy ?? new EmailMessageValidator();
        }

        public static EmailMessageHandler Create(IEmailSender<IEmailMessage> emailSender, IEmailMessage template = null) =>
            new EmailMessageHandler(emailSender, template == null ? null : Options.Create(template));

        public EmailMessageHandler SetStrategy(IValidationStrategy<IEmailMessage> validator)
        {
            _validationStrategy = validator ?? throw new ArgumentNullException(nameof(validator));
            return this;
        }

        public void Send(CancellationToken cancellationToken = default) =>
            SendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public bool TrySend(CancellationToken cancellationToken = default) =>
            TrySendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            if (!await ValidateAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The email failed validation and was not sent.");
            await _emailSender.SendAsync(Email, cancellationToken).ConfigureAwait(false);
            Email = (Template ?? EmailMessage.Default).Copy();
        }

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default)
        {
            if (!await ValidateAsync(cancellationToken).ConfigureAwait(false))
                return false;
            bool isSent = await _emailSender.TrySendAsync(Email, cancellationToken).ConfigureAwait(false);
            if (isSent)
                Email = (Template ?? EmailMessage.Default).Copy();
            return isSent;
        }

        private async Task<bool> ValidateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = _validationStrategy is ICancellableValidationStrategy<IEmailMessage> cancellable ?
                await cancellable.ValidateAsync(Email, cancellationToken).ConfigureAwait(false) :
                await _validationStrategy.ValidateAsync(Email).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
    }
}
