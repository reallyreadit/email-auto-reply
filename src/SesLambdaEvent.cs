namespace EmailAutoReply;

public class SesLambdaEventCommonHeaders {
	public string[] From { get; set; } = new string[0];
	public string? MessageId { get; set; }
	public string? Subject { get; set; }
}

public class SesLambdaEventHeader {
	public string? Name { get; set; }
	public string? Value { get; set; }
}

public class SesLambdaEventMailData {
	public SesLambdaEventCommonHeaders CommonHeaders { get; set; } = new SesLambdaEventCommonHeaders();
	public SesLambdaEventHeader[] Headers { get; set; } = new SesLambdaEventHeader[0];
}

public class SesLambdaEventSesData {
	public SesLambdaEventMailData Mail { get; set; } = new SesLambdaEventMailData();
}

public class SesLambdaEventRecord {
	public SesLambdaEventSesData Ses { get; set; } = new SesLambdaEventSesData();
}

public class SesLambdaEvent {
	public SesLambdaEventRecord[] Records { get; set; } = new SesLambdaEventRecord[0];
}