using System;
using System.Text.Json;
using Verlite;

[assembly: CLSCompliant(true)]

if (args.Length == 0)
{
	var selfExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? throw new NotSupportedException();
	var selfDir = Environment.CurrentDirectory;

	var testArgs = new string[]
	{
		"test",
		"te\\st",
		"\"te\\st\"",
		"some spaces",
		"some\nnewlines",
		"some\rcarriages",
		"some\"quotes",
		"some\'apostrophe",
		"\"some\'quotedapostrophe\"",
		"some\ttabs",
		"finalbackslash\\",
		"finalbackslashquote\\\"",
	};

	var (stdout, _) = await Command.Run(selfDir, selfExe, testArgs);

	var resultArgs = JsonSerializer.Deserialize<string[]>(stdout) ?? throw new JsonException("Invalid json returned.");

	int argsDidntMatch()
	{
		var jsonOpts = new JsonSerializerOptions()
		{
			AllowTrailingCommas = true,
			WriteIndented = true,
		};

		Console.Error.WriteLine("Error! Returned args did not exactly match, got:");
		Console.Error.WriteLine(JsonSerializer.Serialize<string[]>(resultArgs, jsonOpts));
		Console.Error.WriteLine("expected:");
		Console.Error.WriteLine(JsonSerializer.Serialize<string[]>(testArgs, jsonOpts));
		return 1;
	}

	if (resultArgs.Length != testArgs.Length)
		return argsDidntMatch();

	for (int i = 0; i < resultArgs.Length; i++)
		if (resultArgs[i] != testArgs[i])
			return argsDidntMatch();

	return 0;
}
else
{
	Console.WriteLine(JsonSerializer.Serialize<string[]>(args));
	return 0;
}
