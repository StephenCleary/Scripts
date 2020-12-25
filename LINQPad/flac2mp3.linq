<Query Kind="Statements" />

var folder = @"C:\Users\steph\Downloads\rips\audio\Roald Dahl\Charlie and the Chocolate Factory";

foreach (var file in Directory.EnumerateFiles(folder, "*.flac"))
{
	var outputName = Path.ChangeExtension(file, "mp3");
	var result = await ProcessEx.ExecuteAsync("ffmpeg", $"-i \"{file}\" -b:a 128k \"{outputName}\"");
	result.Dump();
}