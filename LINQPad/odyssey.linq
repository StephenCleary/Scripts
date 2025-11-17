<Query Kind="Statements">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
</Query>

var client = new HttpClient();
var urls = File.ReadAllLines(@"C:\Work\DownloadMrRogers\Odyssey\raw-urls.txt")
	.Distinct();

foreach (var url in urls)
{
	using var request = GetRequest(url);
	using var response = client.Send(request);
	response.EnsureSuccessStatusCode();
	using var stream = response.Content.ReadAsStream();
	stream.CopyTo(File.OpenWrite(Path.Combine(@"C:\Work\DownloadMrRogers\Odyssey", Path.GetFileName(new Uri(url).LocalPath))));
}

HttpRequestMessage GetRequest(string uri)
{
	var result = new HttpRequestMessage(HttpMethod.Get, uri);
	result.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
	result.Headers.Add("authority", "media.adventuresinodyssey.com");
	result.Headers.Add("accept", "*/*");
	result.Headers.Add("accept-encoding", "identity;q=1, *;q=0");
	result.Headers.Add("accept-language", "en-US,en;q=0.9");
	result.Headers.Add("cache-control", "no-cache");
	result.Headers.Add("dnt", "1");
	result.Headers.Add("pragma", "no-cache");
	result.Headers.Add("priority", "i");
	result.Headers.Add("referer", "https://app.adventuresinodyssey.com/");
	result.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"128\", \"Not; A = Brand\";v=\"24\", \"Google Chrome\";v=\"128\"");
	result.Headers.Add("sec-ch-ua-mobile", "?0");
	result.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
	result.Headers.Add("sec-fetch-dest", "audio");
	result.Headers.Add("sec-fetch-mode", "no-cors");
	result.Headers.Add("sec-fetch-site", "same-site");
	result.Headers.Add("range", "bytes=0-");
	return result;
}