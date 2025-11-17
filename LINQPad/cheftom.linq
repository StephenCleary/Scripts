<Query Kind="Statements">
  <NuGetReference>AngleSharp</NuGetReference>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>AngleSharp</Namespace>
</Query>

var url = @"https://www.tadl.org/posts?&field_bl_tags_target_id%5B339%5D=339";
var downloadFolder = @"C:\Work\ChefTom";

var client = new HttpClient();
var browser = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

while (true)
{
	var html = await browser.OpenAsync(url.Dump());
	var postUris = html.QuerySelectorAll("a:has(h2)")
		.Select(x => x.GetAttribute("href") ?? throw new InvalidOperationException())
		.Select(x => new Uri(new Uri(html.BaseUri), x).ToString())
		.ToList();
	foreach (var uri in postUris)
	{
		var post = await browser.OpenAsync(uri);
		var pdfUriElement = post.QuerySelectorAll("a")
			.Where(x => x.GetAttribute("type") == "application/pdf")
			.SingleOrDefault();
		if (pdfUriElement == null)
		{
			uri.Dump("Did not find pdf");
		}
		else
		{
			var pdfUri = new Uri(new Uri(post.BaseUri), pdfUriElement.GetAttribute("href") ?? throw new InvalidOperationException()).ToString();
			var filename = Path.Combine(downloadFolder, $"{Path.GetFileName(Path.GetDirectoryName(pdfUri))}-{Path.GetFileName(pdfUri)}");
			if (File.Exists(filename))
			{
				filename.Dump("Exists!");
			}
			else
			{
				using var downloadStream = await client.GetStreamAsync(pdfUri);
				using var outputStream = File.OpenWrite(filename);
				await downloadStream.CopyToAsync(outputStream);
			}
		}
		
		var title = post.QuerySelectorAll("h1")
			.Single()
			.TextContent
			.Trim();
		var imageElement = post.QuerySelectorAll("main img")
			.SingleOrDefault();
		if (imageElement == null)
		{
			uri.Dump("Did not find image");
		}
		else
		{
			var imageUri = new Uri(new Uri(post.BaseUri), imageElement.GetAttribute("src") ?? throw new InvalidOperationException()).ToString();
			var imageFilename = Path.Combine(downloadFolder, $"{Path.GetFileName(Path.GetDirectoryName(imageUri))}-{title}.{Path.GetExtension(new UriBuilder(imageUri).Path)}");
			if (File.Exists(imageFilename))
			{
				imageFilename.Dump("Exists!");
			}
			else
			{
				using var downloadStream = await client.GetStreamAsync(imageUri);
				using var outputStream = File.OpenWrite(imageFilename);
				await downloadStream.CopyToAsync(outputStream);
			}
		}
	}

	var nextPageLinkElement = html.QuerySelectorAll("a")
		.Where(x => x.GetAttribute("rel") == "next")
		.SingleOrDefault();
	if (nextPageLinkElement == null)
	{
		"Done!".Dump();
		break;
	}
	
	url = new Uri(new Uri(html.BaseUri), nextPageLinkElement.GetAttribute("href") ?? throw new InvalidOperationException()).ToString();
}