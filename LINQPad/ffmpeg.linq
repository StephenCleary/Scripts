<Query Kind="Statements" />

var folder = @"Z:\Media\Dick van Dyke\Season 4";

foreach (var file in Directory.EnumerateFiles(folder, "*.mkv"))
{
	var filename = Path.GetFileName(file);
	var split = Regex.Match(filename, @"^S(\d\d)E(\d\d).*$");
	if (!split.Success || split.Groups.Count != 3 || file.EndsWith("flac.mkv", StringComparison.InvariantCultureIgnoreCase) || file.IndexOf("commentary", StringComparison.InvariantCultureIgnoreCase) == -1)
	{
		$"Did not match {file}".Dump();
		continue;
	}

	var season = int.Parse(split.Groups[1].Value);
	var episode = int.Parse(split.Groups[2].Value);
	if (episode != 27 && episode != 29)
		continue;
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