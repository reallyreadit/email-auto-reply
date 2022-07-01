using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EmailAutoReply;

public static class LambdaHandler {
	[LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
	public static async Task Invoke(SesLambdaEvent sesEvent) {
		// Don't send replies to auto-generated messages.
		var autoSubmittedHeader = sesEvent.Records[0].Ses.Mail.Headers.SingleOrDefault(
			header => header.Name?.ToLower() == "auto-submitted"
		);
		if (autoSubmittedHeader is not null && autoSubmittedHeader.Value?.ToLower() != "no") {
			return;
		}

		// Get the common headers.
		var commonHeaders = sesEvent.Records[0].Ses.Mail.CommonHeaders;
		if (commonHeaders.Subject is null || commonHeaders.MessageId is null) {
			return;
		}

		// Don't send replies to our own addresses.
		var recipients = commonHeaders.From
			.Where(
				address => !Regex.IsMatch(input: address, pattern: @"@(readup\.(com|org)|reallyread\.it)($|>)")
			)
			.ToArray();
		
		// Verify that we only have a single recipient.
		if (recipients.Length != 1) {
			return;
		}
		var recipient = recipients[0];

		// Check the dispatch logs to avoid sending too many emails to a single recipient.
		var dynamoTableName = "ses-auto-reply-dispatch";
		var dynamoEmailAddressKey = "email-address";
		var dynamoLastDispatchKey = "last-dispatch";
		var dynamoClient = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
		var dispatchResult = await dynamoClient.GetItemAsync(
			dynamoTableName,
			new Dictionary<string, AttributeValue> {
				{ dynamoEmailAddressKey, new AttributeValue(recipient) }
			}
		);
		var now = DateTime.UtcNow;
		if (dispatchResult.IsItemSet) {
			var lastSent = DateTime.Parse(dispatchResult.Item[dynamoLastDispatchKey].S, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
			if (
				now.Subtract(lastSent).TotalHours < 24
			) {
				return;
			}
		}

		// Record the latest dispatch.
		await dynamoClient.PutItemAsync(
			dynamoTableName,
			new Dictionary<string, AttributeValue> {
				{ dynamoEmailAddressKey, new AttributeValue(recipient) },
				{ dynamoLastDispatchKey, new AttributeValue(now.ToString("o")) }
			}
		);

		// Send the reply.
		var sesClient = new AmazonSimpleEmailServiceClient(RegionEndpoint.USEast1);
		await sesClient.SendRawEmailAsync(
			new SendRawEmailRequest(
				new RawMessage(
					MessageCreator.CreateRawAutoReplyMessage(
						recipient: recipient,
						originalSubject: commonHeaders.Subject,
						originalMessageId: commonHeaders.MessageId
					)
				)
			)
		);
	}
}