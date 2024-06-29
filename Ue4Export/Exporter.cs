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
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise;
using CUE4Parse_Conversion.Textures;
using Newtonsoft.Json;
using SkiaSharp;
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
		private readonly string mAssetListPath;
		private readonly string mOutDir;
		private readonly FAesKey mAesKey;

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

		public Exporter(Options options, Logger? logger)
		{
			mGameDir = options.PaksDirectory;
			mAssetListPath = options.AssetListFile;
			mOutDir = options.OutputDirectory;
			mAesKey = options.EncryptionKey is null ? new(new byte[32]) : new(options.EncryptionKey);
			mLogger = logger;

			mJsonSettings = new JsonSerializerSettings()
			{
				Formatting = Formatting.Indented
			};
		}

		public bool Export()
		{
			bool success = true;

			using (var provider = new DefaultFileProvider(mGameDir, SearchOption.TopDirectoryOnly))
			{
				provider.Initialize();

				foreach (var vfsReader in provider.UnloadedVfs)
				{
					provider.SubmitKey(vfsReader.EncryptionKeyGuid, mAesKey);
				}

				provider.LoadLocalization(ELanguage.English);

				ExportFormats formats = ExportFormats.Auto;
				mLogger?.Log(LogLevel.Important, "Export format is now [Auto]");

				foreach (string line in File.ReadAllLines(mAssetListPath))
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

						ExportFormats oldFormats = formats;
						formats = ParseFormats(trimmed[1..^1]);

						if (oldFormats != formats)
						{
							mLogger?.Log(LogLevel.Important, $"Export format is now [{formats.ToString().Replace(" |", ",")}]");
						}

						continue;
					}

					if (!FindAndExportAssets(provider, formats, trimmed))
					{
						success = false;
					}
				}
			}

			return success;
		}

		private ExportFormats ParseFormats(string input)
		{
			ExportFormats formats = ExportFormats.None;

			bool checkIsAuto()
			{
                if (formats == ExportFormats.Auto)
				{
					mLogger?.Log(LogLevel.Warning, "[Auto] format cannot be combined with other formats in the same group. Other formats will be ignored.");
					return true;
				}
				return false;
            }

			string[] headers = input.Split(',');
			foreach (string h in headers)
			{
				string header = h.Trim().ToLowerInvariant();
				switch (header)
				{
					case "auto":
						if (formats != ExportFormats.None && formats != ExportFormats.Auto)
						{
							mLogger?.Log(LogLevel.Warning, "[Auto] format cannot be combined with other formats in the same group. Other formats will be ignored.");
						}
						formats = ExportFormats.Auto;
						break;
					case "text":
						if (!checkIsAuto())
						{
							formats |= ExportFormats.Text;
						}
						break;
					case "raw":
						if (!checkIsAuto())
						{
							formats |= ExportFormats.Raw;
						}
						break;
					case "texture":
						if (!checkIsAuto())
						{
							formats |= ExportFormats.Texture;
						}
						break;
					default:
						mLogger?.Log(LogLevel.Error, $"Unrecognized export format {input}");
						return ExportFormats.None;
				}
			}

			return formats;
		}

		private bool FindAndExportAssets(AbstractVfsFileProvider provider, ExportFormats formats, string searchPattern)
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
					success &= ExportAsset(provider, formats, assetPath, true);
				}
				return success;
			}
			else
			{
				return ExportAsset(provider, formats, searchPattern, false);
			}
		}

		private bool ExportAsset(AbstractVfsFileProvider provider, ExportFormats formats, string assetPath, bool isBulkExport)
		{
			mLogger?.Log(LogLevel.Information, $"Exporting {assetPath}...");

			bool ret = true;

			try
			{
				if ((formats & ExportFormats.Auto) != 0)
				{
					SaveAuto(provider, assetPath, isBulkExport);
				}
				if ((formats & ExportFormats.Raw) != 0)
				{
					SaveRaw(provider, assetPath);
				}
				if ((formats & ExportFormats.Text) != 0)
				{
					ret = ret && SaveText(provider, assetPath, true, isBulkExport);
				}
				if ((formats & ExportFormats.Texture) != 0)
				{
					ret = ret && SaveTexture(provider, assetPath, true, isBulkExport);
				}
			}
			catch (Exception ex)
			{
				mLogger?.Log(LogLevel.Warning, $"Export of asset {assetPath} failed! [{ex.GetType().FullName}] {ex.Message}");
				ret = false;
			}

			return ret;
		}

		private void SaveAuto(AbstractVfsFileProvider provider, string assetPath, bool isBulkExport)
		{
			bool saved =
				SaveTexture(provider, assetPath, false, false) ||
				SaveText(provider, assetPath, false, false);

			if (!saved)
			{
				SaveRaw(provider, assetPath);
			}
		}

		private void SaveRaw(AbstractVfsFileProvider provider, string assetPath)
		{
			string ext = GetTrimmedExtension(assetPath);

			switch (ext)
			{
				case "bin":
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
				case "png":
				case "jpg":
				case "bmp":
				case "svg":
				case "ufont":
				case "otf":
				case "ttf":
				case "wem":
				case "locmeta":
				case "locres":
					{
						byte[] data = provider.Files[assetPath].Read();

						string outPath = Path.Combine(mOutDir, assetPath);
						Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
						File.WriteAllBytes(outPath, data);

						break;
					}
				default:
					{
						// This cases catches the most common types like uasset, umap, etc.

						// Need to trim off the file extension or the package won't be found
						int extPos = assetPath.LastIndexOf('.');
						if (extPos < 1) extPos = assetPath.Length;

						var raw = provider.SavePackage(assetPath[..extPos]);
						foreach (var pair in raw)
						{
							string outPath = Path.Combine(mOutDir, pair.Key);
							Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
							File.WriteAllBytes(outPath, pair.Value);
						}

						break;
					}
			}
		}

		private bool SaveText(AbstractVfsFileProvider provider, string assetPath, bool logErrors, bool isBulkExport)
		{
			string ext = GetTrimmedExtension(assetPath);
			string? text = null;
			bool changeExtenstionToJson = true;

			switch (ext)
			{
				case "":
				case "uasset":
				case "umap":
					{
						var exports = provider.LoadAllObjects(assetPath);
						text = JsonConvert.SerializeObject(exports, mJsonSettings);
						break;
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
					text = Encoding.UTF8.GetString(provider.Files[assetPath].Read());
					changeExtenstionToJson = false;
					break;
				case "locmeta":
					text = SerializeObject<FTextLocalizationMetaDataResource>(provider, assetPath);
					break;
				case "locres":
					text = SerializeObject<FTextLocalizationResource>(provider, assetPath);
					break;
				case "bin" when assetPath.Contains("AssetRegistry"):
					text = SerializeObject<FAssetRegistryState>(provider, assetPath);
					break;
				case "bnk":
				case "pck":
					text = SerializeObject<WwiseReader>(provider, assetPath);
					break;
				case "udic":
					text = SerializeObject<FOodleDictionaryArchive>(provider, assetPath);
					break;
				case "ushaderbytecode":
				case "ushadercode":
					text = SerializeObject<FShaderCodeArchive>(provider, assetPath);
					break;
				case "png":
				case "jpg":
				case "bmp":
				case "svg":
				case "ufont":
				case "otf":
				case "ttf":
				case "wem":
				default:
					if (!isBulkExport) mLogger?.Log(LogLevel.Warning, $"{assetPath} - This asset cannot be converted to Text.");
					return isBulkExport;
			}

			string outPath;
			if (changeExtenstionToJson)
			{
				outPath = Path.Combine(mOutDir, Path.ChangeExtension(assetPath, ".json"));
			}
			else
			{
				outPath = Path.Combine(mOutDir, assetPath);
			}

			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, text);

			return true;
		}

		private string SerializeObject<T>(AbstractVfsFileProvider provider, string assetPath)
		{
			using FArchive archive = provider.CreateReader(assetPath);
			object obj = Activator.CreateInstance(typeof(T), archive)!;
			return JsonConvert.SerializeObject(obj, mJsonSettings);
		}

		private bool SaveTexture(AbstractVfsFileProvider provider, string assetPath, bool logErrors, bool isBulkExport)
		{
			string ext = GetTrimmedExtension(assetPath);

			switch (ext)
			{
				case "":
				case "uasset":
					{
						IEnumerable<UObject> objects = provider.LoadAllObjects(assetPath);

						bool textureFound = false;
						bool success = true;

						foreach (UObject obj in objects)
						{
							UTexture2D? texture = obj as UTexture2D;
							if (texture == null) continue;

							textureFound = true;

							SKBitmap? bitmap = texture.Decode();
							if (bitmap == null)
							{
								mLogger?.Log(LogLevel.Warning, $"{texture.GetPathName()} - Failed to decode texture.");
								success = false;
								continue;
							}

							if (!texture.SRGB)
							{
								SKColor[] pixels = bitmap.Pixels;
								for (int i = 0; i < pixels.Length; ++i)
								{
									pixels[i] = LinearToSrgb(pixels[i]);
								}
								bitmap.Pixels = pixels;
							}

							mLogger?.Log(LogLevel.Information, $"  Saving texture {texture.Name}");

							string outPath = Path.Combine(mOutDir, $"{ConvertAssetPath(texture.GetPathName())}.png");
							success &= WriteTexture(bitmap, SKEncodedImageFormat.Png, outPath);
						}

						if (logErrors && !textureFound)
						{
							if (isBulkExport)
							{
								mLogger?.Log(LogLevel.Information, $"  No texture to export");
							}
							else
							{
								mLogger?.Log(LogLevel.Warning, $"{assetPath} - No textures found in asset.");
							}
						}

						return success && (textureFound || isBulkExport);
					}
				case "png":
				case "jpg":
				case "bmp":
					{
						byte[] data = provider.SaveAsset(assetPath);

						string outPath = Path.Combine(mOutDir, assetPath);

						SKEncodedImageFormat format;
						switch (ext)
						{
							case "bmp":
								format = SKEncodedImageFormat.Bmp;
								break;
							case "jpg":
								format = SKEncodedImageFormat.Jpeg;
								break;
							case "png":
							default:
								format = SKEncodedImageFormat.Png;
								break;
						}

						using (MemoryStream stream = new(data))
						{
							SKBitmap bitmap = SKBitmap.Decode(stream);
							if (bitmap == null)
							{
								mLogger?.Log(LogLevel.Warning, $"{assetPath} - Failed to decode texture.");
								return false;
							}

							return WriteTexture(bitmap, format, outPath);
						}
					}
				default:
					if (logErrors && !isBulkExport) mLogger?.Log(LogLevel.Warning, $"{assetPath} - This asset cannot be saved as a texture.");
					return false;
			}
		}

		private static bool WriteTexture(SKBitmap bitmap, SKEncodedImageFormat outFormat, string outPath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

			SKData data = bitmap.Encode(outFormat, 100);
			if (data == null)
			{
				return false;
			}

			using (FileStream file = File.Create(outPath))
			{
				data.SaveTo(file);
			}

			return true;
		}

		/// <summary>
		/// If asset file name is the same as asset name, remove asset name from the path.
		/// Also converts path separators to OS format.
		/// </summary>
		private static string ConvertAssetPath(string assetPath)
		{
			string? dir = Path.GetDirectoryName(assetPath);
			string? file = Path.GetFileName(assetPath);

			if (file is null) return assetPath;

			string[] parts = file.Split('.');
			if (parts.Length > 1)
			{
				if (parts[0] == parts[1])
				{
					parts[1] = string.Empty;
				}
			}
			file = string.Join(".", parts.Where(p => !string.IsNullOrEmpty(p)));

			if (dir is null)
			{
				return file;
			}

			return Path.Combine(dir, file);
		}

		/// <summary>
		/// Returns the file extension from a path without a leading period
		/// </summary>
		private static string GetTrimmedExtension(string path)
		{
			string ext = Path.GetExtension(path);
			if (ext.StartsWith('.')) ext = ext[1..];
			return ext;
		}

		private const float FloatToInt = 255.0f;
		private const float IntToFloat = 1.0f / FloatToInt;

		private static SKColor LinearToSrgb(SKColor linearColor)
		{
			return new SKColor(LinearToSrgb(linearColor.Red * IntToFloat), LinearToSrgb(linearColor.Green * IntToFloat), LinearToSrgb(linearColor.Blue * IntToFloat), linearColor.Alpha);
		}

		private static byte LinearToSrgb(float linear)
		{
			if (linear <= 0.0f) return 0;
			if (linear <= 0.00313066844250063f) return (byte)(linear * 12.92f * FloatToInt);
			if (linear < 1) return (byte)((1.055f * Math.Pow(linear, 1.0f / 2.4f) - 0.055f) * FloatToInt);
			return 255;
		}

		[Flags]
		private enum ExportFormats
		{
			None = 0x00,
			Auto = 0x01,
			Raw = 0x02,
			Text = 0x04,
			Texture = 0x08
		}
	}
}
