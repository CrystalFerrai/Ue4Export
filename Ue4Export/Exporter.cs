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
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace Ue4Export
{
	internal class Exporter
	{
		private readonly string mGameDir;
		private readonly string mOutDir;

		private readonly TextWriter mInfoOut;
		private readonly TextWriter mErrorOut;

		private readonly JsonSerializerSettings mJsonSettings;

		public Exporter(string gameDir, string outDir, TextWriter infoOut, TextWriter errorOut)
		{
			mGameDir = gameDir;
			mOutDir = outDir;

			mInfoOut = infoOut;
			mErrorOut = errorOut;

			mJsonSettings = new JsonSerializerSettings()
			{
				Formatting = Formatting.Indented
			};
		}

		public bool Export(string assetListPath)
		{
			var provider = new DefaultFileProvider(mGameDir, SearchOption.TopDirectoryOnly);
			provider.Initialize();

			foreach (var vfsReader in provider.UnloadedVfs)
			{
				provider.SubmitKey(vfsReader.EncryptionKeyGuid, new FAesKey(new byte[32]));
			}

			provider.LoadMappings();
			provider.LoadLocalization(ELanguage.English);

			bool success = true;
			ExportFormat format = ExportFormat.Json;

			foreach (string line in File.ReadAllLines(assetListPath))
			{
				if (string.IsNullOrWhiteSpace(line)) continue;

				string trimmed = line.Trim();
				if (trimmed.StartsWith('#')) continue;

				if (trimmed.StartsWith('['))
				{
					if (trimmed.Equals("[Json]", StringComparison.InvariantCultureIgnoreCase))
					{
						format = ExportFormat.Json;
					}
					else if (trimmed.Equals("[Raw]", StringComparison.InvariantCultureIgnoreCase))
					{
						format = ExportFormat.Raw;
					}
					else
					{
						mErrorOut.WriteLine($"Unrecognized header {trimmed}");
						return false;
					}
					continue;
				}

				if (!ExportAsset(provider, format, trimmed))
				{
					success = false;
				}
			}

			return success;
		}

		private bool ExportAsset(AbstractVfsFileProvider provider, ExportFormat format, string assetPath)
		{
			mInfoOut.WriteLine($"Exporting {format}: {assetPath}...");

			try
			{
				var exports = provider.LoadObjectExports(assetPath);

				switch (format)
				{
					case ExportFormat.Raw:
						{
							var raw = provider.SavePackage(assetPath);
							foreach (var pair in raw)
							{
								string outPath = Path.Combine(mOutDir, pair.Key);
								Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
								File.WriteAllBytes(outPath, pair.Value);
							}

							break;
						}
					case ExportFormat.Json:
						{
							string json = JsonConvert.SerializeObject(exports, mJsonSettings);

							string outPath = Path.Combine(mOutDir, $"{assetPath}.json");
							Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
							File.WriteAllText(outPath, json);

							break;
						}
				}
			}
			catch (Exception ex)
			{
				mErrorOut.WriteLine($"  ERROR: Asset export failed! [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			return true;
		}

		private enum ExportFormat
		{
			Raw,
			Json
		}
	}
}
