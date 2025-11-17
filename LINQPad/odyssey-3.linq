<Query Kind="Statements">
  <Namespace>System.Text.Json</Namespace>
</Query>

foreach (var jsonFilename in Directory.EnumerateFiles(@"C:\Work\DownloadMrRogers\Odyssey", "*.json"))
{
	var json = JsonDocument.Parse(File.ReadAllText(jsonFilename)).RootElement;
	var baseName = Path.GetFileNameWithoutExtension(new Uri(json.GetProperty("download_url").GetString()).LocalPath);
	var downloadFilename = Path.ChangeExtension(baseName, "json");
	if (!Directory.EnumerateFiles(@"C:\Work\DownloadMrRogers\Odyssey\done").Any(x => Path.GetFileNameWithoutExtension(x) == baseName))
		baseName.Dump();
	File.Copy(jsonFilename, Path.Combine(@"C:\Work\DownloadMrRogers\Odyssey\done", downloadFilename));
}