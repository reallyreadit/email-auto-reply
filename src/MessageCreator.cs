using System.Text.RegularExpressions;
using MimeKit;

namespace EmailAutoReply;

public static class MessageCreator {
	public static MemoryStream CreateRawAutoReplyMessage(string recipient, string originalSubject, string originalMessageId) {
		var message = new MimeMessage();
		message.From.Add(
			new MailboxAddress(
				name: "Readup",
				address: "no-reply@readup.org"
			)
		);
		message.To.Add(
			MailboxAddress.Parse(recipient)
		);
		message.Subject = "Re: " + Regex.Replace(originalSubject, @"^re\: ", String.Empty, RegexOptions.IgnoreCase);

		var body = new BodyBuilder {
			TextBody = "This email address is unmonitored. Please join us in our Discord server instead: https://discord.gg/XQZa8pHdVs",
			HtmlBody = "This email address is unmonitored. Please join us in our Discord server instead: <a href=\"https://discord.gg/XQZa8pHdVs\">https://discord.gg/XQZa8pHdVs</a>"
		};
		message.Body = body.ToMessageBody();

		message.Headers.Add(HeaderId.InReplyTo, originalMessageId);
		message.Headers.Add(HeaderId.References, originalMessageId);
		message.Headers.Add(HeaderId.AutoSubmitted, "auto-replied");

		using var stream = new MemoryStream();
		message.WriteTo(stream);
		stream.Position = 0;
		return stream;
	}
}