<Query Kind="Statements" />

var folder = @"E:\encoded\Hogan's Heroes\Season 1";

foreach (var file in Directory.EnumerateFiles(folder, "*.mkv"))
{
	var filename = Path.GetFileName(file);
	$"Processing {filename}...".Dump();

	var outputName = Path.Combine(Path.GetDirectoryName(file), "done", filename);
	var result = await ProcessEx.ExecuteAsync("ffmpeg", $"-i \"{file}\" -map 0 -map -v -map V -c copy -fflags +shortest -max_interleave_delta 0 \"{outputName}\"");
	// `-map 0` Map all streams from input 0
	// `-map -v` Ignore all video streams
	// `-map V` Map all non-cover art video streams
	// `-c copy` Use the copy codec
	// `-fflags +shortest` Stop at the shortest stream
	// `-max_interleave_delta 0` I mean it; really stop at the shortest stream

	//result.Dump();
	$"Done with {filename}".Dump();
}