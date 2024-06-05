using MiEnumGen;

foreach (var file in GetCodepointsFiles("./"))
{
	string name = Path.GetFileNameWithoutExtension(file.Name);

	string destinationPath = Path.ChangeExtension(file.Name, "cs");
	if (file.DirectoryName is { Length: > 0 } destinationDir)
	{
		destinationPath = Path.Combine(destinationDir, destinationPath);
	}

	await using var writer = new StreamWriter(destinationPath);

	await WriteStartEnumAsync(writer, name);

	var parser = new CodepointParser(file.OpenRead());
	await foreach (var token in parser.Parse())
	{
		await WriteEnumIdentifierAsync(writer, token);
	}

	await WriteEndEnumAsync(writer);

	writer.Close();
	Console.WriteLine(string.Join('\n', await File.ReadAllLinesAsync(destinationPath)));
}


static IEnumerable<FileInfo> GetCodepointsFiles(string dirPath)
{
	if (new DirectoryInfo(dirPath) is { Exists: true } dir)
	{
		return dir.EnumerateFiles("*.codepoints");
	}

	return [];
}

static async Task WriteStartEnumAsync(StreamWriter writer, string enumName)
{
	await writer.WriteLineAsync($$"""
                                public enum {{enumName}}
                                {
                                """);
}

static async Task WriteEnumIdentifierAsync(StreamWriter writer, CodepointToken token)
{
	var (identifier, description) = token;
	await writer.WriteLineAsync($"""
                                    [Description("\u{description}")]
                                    {identifier},
                                """);
}

static async Task WriteEndEnumAsync(StreamWriter writer)
{
	await writer.WriteLineAsync("}");
}