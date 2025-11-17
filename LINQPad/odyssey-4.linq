<Query Kind="Statements">
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

var imageUrls = new HashSet<string>();

foreach (var jsonFilename in Directory.EnumerateFiles(@"C:\Work\DownloadMrRogers\Odyssey\rawjson", "*.json"))
{
	var json = JsonDocument.Parse(File.ReadAllText(jsonFilename)).RootElement;
	string[] images = [
		json.GetProperty("thumbnail_large").GetString(),
		..json.GetProperty("characters").EnumerateArray().Select(x => x.GetProperty("thumbnail_large").GetString()),
		..json.GetProperty("aio_metadata").GetProperty("characters").EnumerateArray().Select(x => x.GetProperty("thumbnail_large").GetString()),
	];
	foreach (var image in images.Where(x => x != null))
		imageUrls.Add(image);
}

var imageDownloads = new Dictionary<string, string>();
foreach (var url in imageUrls)
{
	imageDownloads.Add(url,
		Path.Combine(@"C:\Work\DownloadMrRogers\Odyssey\images", Path.GetFileName(new Uri(url).LocalPath)));
}

if (imageDownloads.Count != imageDownloads.Values.Distinct().Count())
	throw new InvalidOperationException("Filename conflict.");

var client = new HttpClient();
foreach (var kvp in imageDownloads.Where(x => !File.Exists(x.Value)))
{
	var bytes = await client.GetByteArrayAsync(kvp.Key);
	File.WriteAllBytes(kvp.Value, bytes);
}
