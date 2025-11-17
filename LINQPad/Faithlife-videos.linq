<Query Kind="Statements">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Windows.Documents</Namespace>
  <Namespace>System.Windows.Markup</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

var client = new HttpClient();

//foreach (var folder in Directory.EnumerateDirectories(@"M:\Logos MobileEd"))
//	await ExtractMetatdataAsync(folder);
//
//foreach (var folder in Directory.EnumerateDirectories(@"M:\Logos MobileEd"))
//	RenameFolder(folder);

//foreach (var folder in Directory.EnumerateDirectories(@"M:\Logos MobileEd"))
//	RenameFiles(folder);
RenameFiles(@"M:\Logos MobileEd\BI311 A Biblical Theology of Glory and Exodus");

async Task ExtractMetatdataAsync(string folder)
{
	if (File.Exists($@"{folder}\title.txt"))
		return;

	$"Generating metadata for {folder}".Dump();
	var metadataJson = JsonDocument.Parse(File.ReadAllText($@"{folder}\metadata.json")).RootElement;
	var imageUrl = metadataJson.GetProperty("coverImages").EnumerateArray().First().GetProperty("url").GetString();
	var imageBytesTask = client.GetByteArrayAsync(imageUrl);

	var title = metadataJson.GetProperty("titles").EnumerateArray().First(x => x.GetProperty("language").GetString() == "en").GetProperty("value").GetString();
	if (title.EndsWith(" (Videos)", StringComparison.InvariantCultureIgnoreCase))
		title = title.Substring(0, title.Length - 9);

	var wpfDescription = metadataJson.GetProperty("descriptions").EnumerateArray().First(x => x.GetProperty("language").GetString() == "en").GetProperty("content").GetString();
	var description = LoadWpfText(wpfDescription);

	var episodeTitles = new List<string>();
	var tocJson = JsonDocument.Parse(File.ReadAllText($@"{folder}\tocindex.json")).RootElement;
	foreach (var item in tocJson.EnumerateArray())
		episodeTitles.Add($"{item.GetProperty("title").GetString()}");

	var videoFileCount = Directory.GetFiles(folder, "*.mp4").Length;
	if (episodeTitles.Count != videoFileCount)
	{
		if (episodeTitles.Count != 0 && episodeTitles[0] == "Overview")
			episodeTitles.RemoveAt(0);

		if (episodeTitles.Count != videoFileCount)
			throw new InvalidOperationException("Invalid number of episode titles.");
	}

	File.WriteAllText($@"{folder}\title.txt", title);
	File.WriteAllText($@"{folder}\summary.txt", description);
	File.WriteAllText($@"{folder}\episodes.txt", string.Join("\n", episodeTitles));
	var imageBytes = await imageBytesTask;
	File.WriteAllBytes($@"{folder}\folder{Path.GetExtension(imageUrl)}", imageBytes);

	string LoadWpfText(string wpfText)
	{
		wpfText = wpfText
			.Replace(@" FontItalic=""True""", "")
			.Replace(@" FontCapitals=""SmallCaps""", "");
		var stringReader = new StringReader($@"<FlowDocument xml:space=""preserve"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">{wpfText}</FlowDocument>");
		var xmlReader = XmlReader.Create(stringReader);
		var element = (FlowDocument)XamlReader.Load(xmlReader);
		return new TextRange(element.ContentStart, element.ContentEnd).Text;
	}
}

void RenameFolder(string folder)
{
	var title = File.ReadAllText($@"{folder}\title.txt");
	var titlePath = PathString(title);
	if (Path.GetFileName(folder) == titlePath)
		return;
	var newName = Path.Combine(Path.GetDirectoryName(folder), titlePath);
	$"Renaming {folder} to {newName}".Dump();
	Directory.Move(folder, newName);
}

void RenameFiles(string folder)
{
	var index = 0;
	var episodeTitles = File.ReadAllLines($@"{folder}\episodes.txt");
	var files = OrderByAlphaNumeric(Directory.GetFiles(folder, "*.mp4")).ToList();
	
	if (episodeTitles.Length != files.Count)
		throw new InvalidOperationException("Invalid number of episodes found.");
	
	var pad = files.Count < 10 ? 1 : files.Count < 100 ? 2 : files.Count < 1000 ? 3 : throw new InvalidOperationException("Too many episodes");
	
	foreach (var file in files)
	{
		var desiredFileName = PathString($"{index.ToString().PadLeft(pad, '0')} {PathString(episodeTitles[index])}");
		if (Path.GetFileNameWithoutExtension(file) != desiredFileName)
		{
			var newName = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(file), desiredFileName), Path.GetExtension(file));
			$"Renaming {file} to {newName}".Dump();
			File.Move(file, newName);
		}
		
		++index;
	}
}

static string PathString(string input) => input.Replace(": ", " - ").Replace(":", ";").Replace("?", "").Replace("/", "-");

static IOrderedEnumerable<string> OrderByAlphaNumeric(IEnumerable<string> source)
{
	int max = source
		.SelectMany(i => Regex.Matches(i, @"\d+").Cast<Match>().Select(m => (int?)m.Value.Length))
		.Max() ?? 0;

	return source.OrderBy(i => Regex.Replace(i, @"\d+", m => m.Value.PadLeft(max, '0')));
}