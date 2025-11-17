<Query Kind="Statements" />

var allUrls = File.ReadAllLines(@"C:\Work\DownloadMrRogers\Odyssey\raw-urls.txt")
	.Distinct()
	.Select(x => $"\"{x}\",");
File.WriteAllLines(@"C:\Work\DownloadMrRogers\Odyssey\urls.txt", allUrls);
// 964/965 precious lord => 970/971