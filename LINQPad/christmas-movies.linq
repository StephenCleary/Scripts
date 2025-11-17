<Query Kind="Statements">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
</Query>

var date = DateTimeOffset.Parse("2025-01-07");

var startTime = Q.StartOfDay(date);
var endTime = Q.EndOfDay(date);

var client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.All, SslOptions = new() { RemoteCertificateValidationCallback = (_, _, _, _) => true } });
var tvguideResponse = await client.GetStringAsync($"https://frndlytv-api.revlet.net/service/api/v1/static/tvguide?channel_ids={string.Join(',', Q.Channels.Keys)}&start_time={startTime.ToUnixTimeMilliseconds()}&end_time={endTime.ToUnixTimeMilliseconds()}");
var jsonResult = JsonDocument.Parse(tvguideResponse);

var results = jsonResult.RootElement.GetProperty("response").GetProperty("data").EnumerateArray()
	.SelectMany(x => x.GetProperty("programs").EnumerateArray().Select(y => new
	{
		Channel = Q.Channels[x.GetProperty("channelId").GetInt32()],
		Title = y.GetProperty("display").GetProperty("title").GetString(),
		Start = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(y.GetProperty("display").GetProperty("markers").GetProperty("startTime").GetProperty("value").GetString())).ToLocalTime(),
		End = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(y.GetProperty("display").GetProperty("markers").GetProperty("endTime").GetProperty("value").GetString())).ToLocalTime(),
	}))
	.Where(x => x.End - x.Start > TimeSpan.FromMinutes(90))
	.OrderBy(x => x.Start).ThenBy(x => x.Channel)
	.Select(x => new { x.Channel, x.Title, Start = x.Start.ToString("MM/dd hh:00tt"), Result = new DumpContainer() })
	.ToList()
	.Dump();

foreach (var result in results)
{
	result.Result.AppendContent("Searching...");
	var json = JsonDocument.Parse(await client.GetStringAsync($"https://api.trello.com/1/search?query={Uri.EscapeDataString(result.Title)}&key=712a7a4ee9c4605509879a4b1bb74b6a&token={Util.GetPassword("Trello token")}&modelTypes=cards&card_list=true&card_fields=name,url&idBoards=652ed1ea978b2b0722c86976&idOrganizations=60bbcc9177a1381bee816b92"));
	result.Result.ClearContent();
	var hits = json.RootElement.GetProperty("cards").EnumerateArray().Select(x => new {
		Url = x.GetProperty("url").GetString(),
		Name = x.GetProperty("name").GetString(),
		ListName = x.GetProperty("list").GetProperty("name").GetString(),
	}).ToList();
	if (hits.Count == 0)
	{
		result.Result.AppendContent(Util.RawHtml("<span class=\"highlight\">*** Not found! ***</span>"));
		continue;
	}

	var searchAsRegex = new Regex($@"^{Regex.Escape(result.Title)} \(\d{{4}}\)$", RegexOptions.IgnoreCase);
	var perfectHit = false;
	if (hits.Count == 1 && searchAsRegex.IsMatch(hits[0].Name))
		perfectHit = true;
	if (hits.Count != 1)
	{
		// See if there's a single perfect hit.
		var bestHits = hits.Where(x => searchAsRegex.IsMatch(x.Name)).ToList();
		if (bestHits.Count == 1)
		{
			hits = bestHits;
			perfectHit = true;
		}
	}

	foreach (var hit in hits)
	{
		var highlight = hit.ListName == "Issues; need re-record" || hit.ListName == "Can Encode but Sketch" || hit.ListName == "Available but not 720p";
		var bold = hit.ListName == "Scheduled to Record" || hit.ListName == "Create AVS";
		var classname = highlight ? "highlight" : bold ? "bold" : null;
		result.Result.AppendContent(new Hyperlinq(hit.Url, perfectHit ? hit.ListName : $"{hit.Name} in {hit.ListName}") { CssClass = classname });
	}
}

static class Q
{
	static public DateTimeOffset StartOfDay(DateTimeOffset now) => new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, 0, now.Offset);
	static public DateTimeOffset EndOfDay(DateTimeOffset now) => StartOfDay(now).AddDays(1);
	static public Dictionary<int, string> Channels = new()
	{
		{ 1, "Hallmark" },
		{ 2, "Hallmark Movies & Mysteries" },
		{ 15, "UpTV" },
		{ 30, "Great American Family" },
	};
}