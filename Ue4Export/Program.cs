// Copyright 2021 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

class Program
{
	static int Main(string[] args)
	{
		if (args.Length != 3)
		{
			Console.WriteLine("Exports a set of files from a UE4 game. Usage:\n\nUe4Export [game asset path] [asset list] [output directory]\n\n  game asset path = path to a directory containing .pak files for a game\n\n  asset list = text file with list of asset paths to export\n\n  output directory = directory to output exported assets");
			return 0;
		}

		string gameDir = args[0];
		if (!Directory.Exists(gameDir))
		{
			Console.Error.WriteLine($"Could not access game directory \"{gameDir}\"");
			return 1;
		}

		string assetListPath = args[1];
		if (!File.Exists(assetListPath))
		{
			Console.Error.WriteLine($"Could not access asset list \"{assetListPath}\"");
			return 1;
		}

		string outDir = args[2];
		try
		{
			Directory.CreateDirectory(outDir);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Could not access/create output directory \"{outDir}\". {ex.GetType().FullName}: {ex.Message}");
			return 1;
		}

		var provider = new DefaultFileProvider(gameDir, SearchOption.TopDirectoryOnly);
		provider.Initialize();

		foreach (var vfsReader in provider.UnloadedVfs)
		{
			provider.SubmitKey(vfsReader.EncryptionKeyGuid, new FAesKey(new byte[32]));
		}

		provider.LoadMappings();
		provider.LoadLocalization(ELanguage.English);

		JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
		{
			Formatting = Formatting.Indented
		};

		int returnValue = 0;

		foreach (string line in File.ReadAllLines(assetListPath))
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			string assetPath = line.Trim();
			if (line.StartsWith('#')) continue;

			Console.Out.WriteLine($"Exporting {assetPath}...");

			try
			{
				var exports = provider.LoadObjectExports(assetPath);
				string json = JsonConvert.SerializeObject(exports, jsonSettings);

				string outPath = Path.Combine(outDir, $"{assetPath}.json");
				Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
				File.WriteAllText(outPath, json);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"  ERROR: Export failed! [{ex.GetType().FullName}] {ex.Message}");
				returnValue = 2;
			}
		}

		Console.Out.WriteLine("Done.");

		// Pause if debugger attached
		if (System.Diagnostics.Debugger.IsAttached) Console.ReadKey();

		return returnValue;
	}
}