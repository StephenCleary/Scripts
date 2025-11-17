<Query Kind="Statements">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Windows.Documents</Namespace>
  <Namespace>System.Windows.Markup</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

var rootFolder = @"D:\Video\Logos MobileEd\_ready";
var client = new HttpClient();
var filenameRegex = new Regex(@"(.*)-videos-1-1000-(.*)\.mp4");

foreach (var folder in Directory.EnumerateDirectories(rootFolder))
{
	folder.Dump();
	await ProcessFolderAsync(folder, client);
}

async Task ProcessFolderAsync(string folder, HttpClient client)
{
	foreach (var file in System.IO.Directory.EnumerateFiles(folder).ToList())
	{
		var match = filenameRegex.Match(Path.GetFileName(file));
		if (!match.Success)
			continue;
		var newFile = file.Replace("-1-1000-", "-1-2400-");

		var url = $"http://resources.logoscdn.com/{match.Groups[1].Value}/videos/1/2400/{match.Groups[2].Value}.mp4";
		url.Dump();

		await SaveToFileAsync(client, url, newFile);
		File.Delete(file);
	}
}

async Task SaveToFileAsync(HttpClient client, string url, string file)
{
	var response = await client.GetAsync(url);
	using var fs = new FileStream(file, FileMode.CreateNew);
	await response.Content.CopyToAsync(fs);
}
