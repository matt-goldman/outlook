using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Zuemail.Core.Abstractions;
using Zuemail.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zuemail.Core.Services
{
    public class EmailMessageValidator : ICancellableValidationStrategy<IEmailMessage>
    {
        private readonly ILogger<EmailMessageValidator> logger;

        public EmailMessageValidator(ILogger<EmailMessageValidator> logger = null)
        {
            this.logger = logger ?? NullLogger<EmailMessageValidator>.Instance;
        }

        public virtual Task<bool> ValidateAsync(IEmailMessage email)
        {
            if (email == null || email.From == null || email.To == null || email.Cc == null ||
                email.Bcc == null || email.ReplyTo == null || email.Headers == null)
                return Task.FromResult(false);
            var from = email.From.ToArray();
            var recipients = email.To.Concat(email.Cc).Concat(email.Bcc).ToArray();
            var contacts = from.Concat(recipients).Concat(email.ReplyTo).ToArray();
            if (contacts.Any(contact => contact == null || HasHeaderDelimiters(contact.Name)) ||
                HasHeaderDelimiters(email.Subject) || email.Headers.Any(header =>
                    !IsHeaderName(header.Key) || header.Value == null || HasHeaderDelimiters(header.Value)))
                return Task.FromResult(false);
            bool isValid = ValidateEmailAddresses(from.Select(contact => contact.EmailAddress),
                recipients.Select(contact => contact.EmailAddress), logger);
            isValid &= email.ReplyTo.All(contact => IsEmailAddress(contact.EmailAddress));
            return Task.FromResult(isValid);
        }

        /// <summary>Validates synchronously while respecting cancellation at entry.</summary>
        /// <param name="email">The message, which is never mutated.</param>
        /// <param name="cancellationToken">Cancels validation.</param>
        /// <returns>Whether the message is valid.</returns>
        public virtual Task<bool> ValidateAsync(IEmailMessage email, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValidateAsync(email);
        }

        public static bool ValidateEmailAddresses(IEnumerable<string> sourceEmailAddresses, IEnumerable<string> destinationEmailAddresses, ILogger logger)
        {
            if (sourceEmailAddresses is null)
                throw new ArgumentNullException(nameof(sourceEmailAddresses));
            if (destinationEmailAddresses is null)
                throw new ArgumentNullException(nameof(destinationEmailAddresses));
            var sources = sourceEmailAddresses.ToArray();
            var destinations = destinationEmailAddresses.ToArray();
            bool isValid = sources.Length > 0 && destinations.Length > 0 &&
                sources.All(IsEmailAddress) && destinations.All(IsEmailAddress);
            if (!isValid)
                (logger ?? NullLogger.Instance).LogWarning("An email requires valid sender and recipient addresses.");
            return isValid;
        }

        private static bool IsEmailAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || HasHeaderDelimiters(address))
                return false;
            try
            {
                return new MailAddress(address).Address.Equals(address.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool HasHeaderDelimiters(string value) =>
            value != null && value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0;

        private static bool IsHeaderName(string value) => !string.IsNullOrEmpty(value) &&
            value.All(character => character >= 33 && character <= 126 && character != ':');
    }
}