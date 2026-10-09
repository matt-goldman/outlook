using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Common;
using Zuemail.Core.Abstractions;

namespace Zuemail.Core.Models
{
    public class EmailMessage : IEmailMessage
    {
        public static EmailMessage Default { get; set; } = new EmailMessage();

        public virtual IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public virtual IList<IEmailContact> From { get; set; } = new List<IEmailContact>();

        public virtual IList<IEmailContact> ReplyTo { get; set; } = new List<IEmailContact>();

        public virtual IList<IEmailContact> To { get; set; } = new List<IEmailContact>();

        public virtual IList<IEmailContact> Cc { get; set; } = new List<IEmailContact>();

        public virtual IList<IEmailContact> Bcc { get; set; } = new List<IEmailContact>();
        
        public virtual IDictionary<string, object> Attachments { get; set; } = new Dictionary<string, object>();

        public virtual IEnumerable<string> AttachmentFilePaths => Attachments.Where(a => a.Value == null).Select(a => a.Key);

        public virtual IEnumerable<string> AttachmentFileNames => AttachmentFilePaths.Select(a => Path.GetFileName(a));

        public virtual string Subject { get; set; } = string.Empty;

        public virtual string BodyText { get; set; } = string.Empty;

        public virtual string BodyHtml { get; set; } = string.Empty;

        public virtual int PreviewLength { get; set; } = 1000;

        private string _bodyPreview = string.Empty;
        public virtual string BodyPreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_bodyPreview) &&
                    !string.IsNullOrWhiteSpace(BodyHtml))
                {
                    _bodyPreview = BodyHtml.DecodeHtml();
                    if (string.IsNullOrWhiteSpace(BodyText))
                        BodyText = _bodyPreview;
                    if (_bodyPreview.Length > PreviewLength)
                        _bodyPreview = $"{_bodyPreview.Substring(0, PreviewLength)}...";
                }
                return _bodyPreview;
            }
            set => _bodyPreview = value;
        }

        /// <summary>Copies mutable message state without reading attachment content.</summary>
        /// <returns>An independent message with copied contacts, collections, and byte arrays.</returns>
        /// <remarks>Streams and other attachment objects remain borrowed caller-owned references.</remarks>
        public virtual IEmailMessage Copy()
        {
            var copy = (EmailMessage)MemberwiseClone();
            copy.Headers = new Dictionary<string, string>(Headers,
                (Headers as Dictionary<string, string>)?.Comparer ?? StringComparer.Ordinal);
            copy.From = From.Select(contact => contact.Copy()).ToList();
            copy.ReplyTo = ReplyTo.Select(contact => contact.Copy()).ToList();
            copy.To = To.Select(contact => contact.Copy()).ToList();
            copy.Cc = Cc.Select(contact => contact.Copy()).ToList();
            copy.Bcc = Bcc.Select(contact => contact.Copy()).ToList();
            copy.Attachments = new Dictionary<string, object>(
                (Attachments as Dictionary<string, object>)?.Comparer ?? StringComparer.Ordinal);
            foreach (var attachment in Attachments)
                copy.Attachments.Add(attachment.Key,
                    attachment.Value is byte[] bytes ? bytes.Clone() : attachment.Value);
            return copy;
        }

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("Date: {0}", DateTimeOffset.Now);
                if (From.Count > 0)
                    text.WriteLine("From: {0}", string.Join("; ", From));
                if (To.Count > 0)
                    text.WriteLine("To: {0}", string.Join("; ", To));
                if (Cc.Count > 0)
                    text.WriteLine("Cc: {0}", string.Join("; ", Cc));
                if (Bcc.Count > 0)
                    text.WriteLine("Bcc: {0}", string.Join("; ", Bcc));
                text.WriteLine("Subject: {0}", Subject);
                if (Attachments.Count > 0)
                    text.WriteLine("{0} Attachment{1}: '{2}'",
                        Attachments.Count, Attachments.Count == 1 ? "" : "s",
                        string.Join("', '", Attachments.Keys));
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
