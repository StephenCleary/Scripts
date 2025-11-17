<Query Kind="Statements">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

var client = new HttpClient();

(string id, string group)[] startIds = [
("a354W0000046TjUQAU", "a314W000000CNrEQAW"), // 1
("a354W0000046U18QAE", "a314W000000CNrFQAW"), // 51
("a354W0000046U5eQAE", "a314W000000CNrGQAW"), // 101
("a354W0000046U9vQAE", "a314W000000CNrHQAW"), // 151
("a354W0000046UElQAM", "a314W000000CNrIQAW"), // 201
("a354W0000046UIYQA2", "a314W000000CNrJQAW"), // 251
("a354W0000046ULDQA2", "a314W000000CNrKQAW"), // 301
("a354W0000046UNxQAM", "a314W000000CNrLQAW"), // 351
("a354W0000046U2rQAE", "a314W000000CNrMQAW"), // 401
("a354W0000046UI6QAM", "a314W000000CNrNQAW"), // 451
("a354W0000046UWBQA2", "a314W000000CNrOQAW"), // 501
("a354W0000046U4DQAU", "a314W000000CNrPQAW"), // 551
("a354W0000046UcYQAU", "a314W000000CNrQQAW"), // 601
("a354W0000046UXZQA2", "a314W000000CNrRQAW"), // 651
("a354W0000046U37QAE", "a314W000000CNrSQAW"), // 701
("a354W0000046UkhQAE", "a314W000000CNrTQAW"), // 751
("a354W0000046TtyQAE", "a314W000000CNrUQAW"), // 801
("a354W0000046UqQQAU", "a314W000000CNrVQAW"), // 851
("a354W0000046UfTQAU", "a314W000000CNrWQAW"), // 901
("a354W0000046qiPQAQ", "a314W000000CVSdQAO"), // 951
];

foreach (var (id, group) in startIds)
{
	var currentId = id;
	int count = 0;
	while (currentId != null)
	{
		var json = await DownloadJsonAsync(currentId, group);
		++count;
		if (!json.TryGetProperty("next_episode", out var nextEpisode))
			break;
		if (nextEpisode.ValueKind == JsonValueKind.Null || !nextEpisode.TryGetProperty("id", out var nextEpisodeId))
			break;		
		currentId = nextEpisodeId.GetString();
	}
	
	if (count != 50)
		new { id, currentId, count }.Dump();
}

string UrlForId(string id, string group) => $"https://fotf.my.site.com/aio/services/apexrest/v1/content/{id}?tag=true&series=true&content_grouping_id={group}&recommendations=true&player=true&parent=true";
async Task<JsonElement> DownloadJsonAsync(string id, string group)
{
	var filename = $@"C:\Work\DownloadMrRogers\Odyssey\{id}.json";

	if (File.Exists(filename))
		return JsonDocument.Parse(File.ReadAllText(filename)).RootElement;
	
	var url = UrlForId(id, group).Dump();
	using var request = new HttpRequestMessage(HttpMethod.Get, url);	
	request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
	request.Headers.Add("authority", "fotf.my.site.com");
	request.Headers.Add("accept", "*/*");
	request.Headers.Add("accept-language", "en-US,en;q=0.9,pl-PL;q=0.8,pl;q=0.7,la;q=0.6");
	// request.Headers.Add("authorization", "Bearer ...");
	request.Headers.Add("cache-control", "no-cache");
	request.Headers.Add("dnt", "1");
	request.Headers.Add("origin", "https://app.adventuresinodyssey.com");
	request.Headers.Add("pragma", "no-cache");
	request.Headers.Add("priority", "u=1, i");
	request.Headers.Add("referer", "https://app.adventuresinodyssey.com/");
	request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"128\", \"Not; A = Brand\";v=\"24\", \"Google Chrome\";v=\"128\"");
	request.Headers.Add("sec-ch-ua-mobile", "?0");
	request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
	request.Headers.Add("sec-fetch-dest", "empty");
	request.Headers.Add("sec-fetch-mode", "cors");
	request.Headers.Add("sec-fetch-site", "cross-site");
	request.Headers.Add("x-experience-name", "Adventures In Odyssey");
	request.Headers.Add("x-viewer-id", "a3JUh000001azh7MAA");
  	using var response = client.Send(request);
	response.EnsureSuccessStatusCode();
	var jsonString = await response.Content.ReadAsStringAsync();
	var json = JsonDocument.Parse(jsonString);
	File.WriteAllText(filename, jsonString);
	await Task.Delay(Random.Shared.Next(1000, 3000));
	return json.RootElement;
}