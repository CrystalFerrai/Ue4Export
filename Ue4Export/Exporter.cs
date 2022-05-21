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
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise;
using Newtonsoft.Json;
using System.Text;

namespace Ue4Export
{
	/// <summary>
	/// Exports Ue4 assets to files
	/// </summary>
	internal class Exporter
	{
		private static readonly HashSet<string> sExtensionsToIgnore;

		private readonly string mGameDir;
		private readonly string mOutDir;

		private readonly Logger? mLogger;

		private readonly JsonSerializerSettings mJsonSettings;

		static Exporter()
		{
			// When exporting with wildcards, filter out uexp/ubulk files because they are not a valid export target and are always
			// paired with something that is valid like a uasset/umap
			sExtensionsToIgnore = new HashSet<string>()
			{
				".uexp",
				".ubulk"
			};
		}

		public Exporter(string gameDir, string outDir, Logger? logger)
		{
			mGameDir = gameDir;
			mOutDir = outDir;
			mLogger = logger;

			mJsonSettings = new JsonSerializerSettings()
			{
				Formatting = Formatting.Indented
			};
		}

		public bool Export(string assetListPath)
		{
			bool success = true;

			using (var provider = new DefaultFileProvider(mGameDir, SearchOption.TopDirectoryOnly))
			{
				provider.Initialize();

				foreach (var vfsReader in provider.UnloadedVfs)
				{
					provider.SubmitKey(vfsReader.EncryptionKeyGuid, new FAesKey(new byte[32]));
				}

				provider.LoadMappings();
				provider.LoadLocalization(ELanguage.English);

				ExportFormats formats = ExportFormats.Json;
				mLogger?.Log(LogLevel.Important, "Export format is now [Json]");

				foreach (string line in File.ReadAllLines(assetListPath))
				{
					if (string.IsNullOrWhiteSpace(line)) continue;

					string trimmed = line.Trim();
					if (trimmed.StartsWith('#')) continue;

					if (trimmed.StartsWith('['))
					{
						if (!trimmed.EndsWith(']'))
						{
							mLogger?.Log(LogLevel.Error, $"{trimmed} - Lines beginning with '[' must end with ']'.");
							return false;
						}

						if (trimmed.Length < 3)
						{
							mLogger?.Log(LogLevel.Error, "[] - Invalid header");
							return false;
						}

						formats = ExportFormats.None;

						string[] headers = trimmed[1..^1].Split(',');
						foreach (string h in headers)
						{
							string header = h.Trim().ToLowerInvariant();
							switch (header)
							{
								case "json":
									formats |= ExportFormats.Json;
									break;
								case "raw":
									formats |= ExportFormats.Raw;
									break;
								default:
									mLogger?.Log(LogLevel.Error, $"Unrecognized header {trimmed}");
									return false;
							}
						}

						mLogger?.Log(LogLevel.Important, $"Export format is now [{formats.ToString().Replace(" |", ",")}]");

						continue;
					}

					if (!SearchAndExportAssets(provider, formats, trimmed))
					{
						success = false;
					}
				}
			}

			return success;
		}

		private bool SearchAndExportAssets(AbstractVfsFileProvider provider, ExportFormats formats, string searchPattern)
		{
			// If there are any wildcards in the path, find all matching assets and export them. Otherwise, just attempt to export it as a single asset.
			if (searchPattern.Any(c => c == '?' || c == '*'))
			{
				var assetPaths = PathSearch.Filter(provider.Files.Keys, searchPattern).Where(p => !sExtensionsToIgnore.Contains(Path.GetExtension(p)));

				if (!assetPaths.Any())
				{
					mLogger?.Log(LogLevel.Warning, $"Could not find any asset matching the path {searchPattern}");
					return false;
				}

				bool success = true;
				foreach (var assetPath in assetPaths)
				{
					success &= ExportAsset(provider, formats, assetPath);
				}
				return success;
			}
			else
			{
				return ExportAsset(provider, formats, searchPattern);
			}
		}

		private bool ExportAsset(AbstractVfsFileProvider provider, ExportFormats formats, string assetPath)
		{
			mLogger?.Log(LogLevel.Information, $"Exporting {assetPath}...");

			try
			{
				if ((formats & ExportFormats.Raw) != 0)
				{
					// Need to trim off the file extension or the package won't be found
					var raw = provider.SavePackage(assetPath[..assetPath.LastIndexOf('.')]);
					foreach (var pair in raw)
					{
						string outPath = Path.Combine(mOutDir, pair.Key);
						Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
						File.WriteAllBytes(outPath, pair.Value);
					}
				}
				if ((formats & ExportFormats.Json) != 0)
				{
					string? json = LoadJson(provider, assetPath);
					if (json == null) return false;

					string outPath = Path.Combine(mOutDir, $"{assetPath}.json");
					Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
					File.WriteAllText(outPath, json);
				}
			}
			catch (Exception ex)
			{
				mLogger?.Log(LogLevel.Warning, $"Export of asset {assetPath} failed! [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			return true;
		}

		private string? LoadJson(AbstractVfsFileProvider provider, string assetPath)
		{
			string ext = Path.GetExtension(assetPath)[1..];

			switch (ext)
			{
				case "uasset":
				case "umap":
					{
						var exports = provider.LoadObjectExports(assetPath);
						return JsonConvert.SerializeObject(exports, mJsonSettings);
					}
				case "ini":
				case "txt":
				case "log":
				case "po":
				case "bat":
				case "dat":
				case "cfg":
				case "ide":
				case "ipl":
				case "zon":
				case "xml":
				case "h":
				case "uproject":
				case "uplugin":
				case "upluginmanifest":
				case "csv":
				case "json":
				case "archive":
				case "manifest":
				case "wem":
					return Encoding.UTF8.GetString(provider.Files[assetPath].Read());
				case "locmeta":
					return ReadObject<FTextLocalizationMetaDataResource>(provider, assetPath);
				case "locres":
					return ReadObject<FTextLocalizationResource>(provider, assetPath);
				case "bin" when assetPath.Contains("AssetRegistry"):
					return ReadObject<FAssetRegistryState>(provider, assetPath);
				case "bnk":
				case "pck":
					return ReadObject<WwiseReader>(provider, assetPath);
				case "udic":
					return ReadObject<FOodleDictionaryArchive>(provider, assetPath);
				case "ushaderbytecode":
				case "ushadercode":
					return ReadObject<FShaderCodeArchive>(provider, assetPath);
				case "png":
				case "jpg":
				case "bmp":
				case "svg":
				case "ufont":
				case "otf":
				case "ttf":
				default:
					mLogger?.Log(LogLevel.Warning, $"{assetPath} - This asset cannot be converted to Json.");
					return null;
			}
		}

		private string ReadObject<T>(AbstractVfsFileProvider provider, string assetPath)
		{
			using FArchive archive = provider.CreateReader(assetPath);
			object obj = Activator.CreateInstance(typeof(T), archive)!;
			return JsonConvert.SerializeObject(obj, mJsonSettings);
		}

		[Flags]
		private enum ExportFormats
		{
			None = 0x00,
			Raw = 0x01,
			Json = 0x02
		}
	}
}
