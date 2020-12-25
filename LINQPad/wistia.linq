<Query Kind="Statements">
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Namespace>System.Windows</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

var client = new HttpClient();
var text = Clipboard.GetText();
var regex = Regex.Match(text, "/([^/]*)\\?wvideo=([^\"]*)\"");
var title = regex.Groups[1].Value;
var videoId = regex.Groups[2].Value;
var containerUri = $"https://fast.wistia.net/embed/iframe/{videoId}?videoFoam=true";
var containerHtml = await client.GetStringAsync(containerUri);
var binaryUri = Regex.Match(containerHtml, "\"(http[^\"]*\\.bin)\"").Groups[1].Value;
var response = await client.GetAsync(binaryUri, HttpCompletionOption.ResponseHeadersRead);
using (var fs = new FileStream(@"C:\Users\steph\Downloads\" + title + ".mp4", FileMode.CreateNew))
	await response.Content.CopyToAsync(fs);