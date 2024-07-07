// Copyright 2024 Crystal Ferrai
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

using CUE4Parse.UE4.Versions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ue4Export
{
	/// <summary>
	/// Program options
	/// </summary>
	internal class Options
	{
		/// <summary>
		/// The directory containing assets to export
		/// </summary>
		public string AssetsDirectory { get; set; }

		/// <summary>
		/// The engine version of the game
		/// </summary>
		public EGame EngineVersion { get; set; }

		/// <summary>
		/// The directory to output exported assets
		/// </summary>
		public string OutputDirectory { get; set; }

		/// <summary>
		/// A file containing the asset list to export
		/// </summary>
		public string AssetListFile { get; set; }

		/// <summary>
		/// Whether to clear the contents of the output directory before starting
		/// </summary>
		public bool MixOutput { get; set; }

		/// <summary>
		/// Path to a mappings file for the game. Needed for UE5 games
		/// </summary>
		public string? MappingsPath { get; set; }

		/// <summary>
		/// The encryption key to use when accessing pak files
		/// </summary>
		public string? EncryptionKey { get; set; }

		/// <summary>
		/// Specifies how to search the assets directory for assets
		/// </summary>
		public SearchOption AssetSearchOption { get; set; }

		/// <summary>
		/// Whether to minimize log output
		/// </summary>
		public bool IsQuiet { get; set; }

		/// <summary>
		/// Whether to eliminate any log output that is not an error
		/// </summary>
		public bool IsSilent { get; set; }

		private Options()
		{
			AssetsDirectory = null!;
			OutputDirectory = null!;
			AssetListFile = null!;
			MixOutput = false;
			EncryptionKey = null;
			AssetSearchOption = SearchOption.AllDirectories;
			IsQuiet = false;
			IsSilent = false;
		}

		/// <summary>
		/// Create an Options instance from command line arguments
		/// </summary>
		/// <param name="args">The command line arguments to parse</param>
		/// <param name="logger">For logging parse errors</param>
		/// <param name="options">Outputs the options if parsing is successful</param>
		/// <returns>Whether parsing was successful</returns>
		public static bool TryParseCommandLine(string[] args, Logger logger, [NotNullWhen(true)] out Options? result)
		{
			if (args.Length == 0)
			{
				result = null;
				return false;
			}

			Options instance = new();

			int positionalArgIndex = 0;

			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i].StartsWith("--"))
				{
					// Explicit arg
					string argValue = args[i][2..];
					switch (argValue)
					{
						case "mix-output":
							instance.MixOutput = true;
							break;
						case "mappings":
							if (i < args.Length - 1 && !args[i + 1].StartsWith("--"))
							{
								instance.MappingsPath = args[i + 1];
								++i;
							}
							else
							{
								logger.LogError("Missing parameter for --mappings argument");
								result = null;
								return false;
							}
							break;
						case "key":
							if (i < args.Length - 1 && !args[i + 1].StartsWith("--"))
							{
								instance.EncryptionKey = args[i + 1];
								++i;
							}
							else
							{
								logger.LogError("Missing parameter for --key argument");
								result = null;
								return false;
							}
							break;
						case "no-subdirs":
							instance.AssetSearchOption = SearchOption.TopDirectoryOnly;
							break;
						case "quiet":
							instance.IsQuiet = true;
							break;
						case "silent":
							instance.IsSilent = true;
							break;
						default:
							logger.LogError($"Unrecognized argument '{args[i]}'");
							result = null;
							return false;
					}
				}
				else
				{
					// Positional arg
					switch (positionalArgIndex)
					{
						case 0:
							instance.AssetsDirectory = Path.GetFullPath(args[i]);
							break;
						case 1:
							{
								string value = args[i];
								if (!value.StartsWith("GAME_", StringComparison.OrdinalIgnoreCase))
								{
									value = "GAME_" + value;
								}
								if (Enum.TryParse<EGame>(value, true, out EGame version))
								{
									instance.EngineVersion = version;
								}
								else
								{
									logger.LogError($"{args[i]} is not a valid engine version.");
									result = null;
									return false;
								}
							}
							break;
						case 2:
							instance.AssetListFile = Path.GetFullPath(args[i]);
							break;
						case 3:
							instance.OutputDirectory = Path.GetFullPath(args[i]);
							break;
						default:
							logger.LogError("Too many positional arguments.");
							result = null;
							return false;
					}
					++positionalArgIndex;
				}
			}

			if (positionalArgIndex < 4)
			{
				logger.LogError($"Not enough positional arguments");
				result = null;
				return false;
			}

			if (!Directory.Exists(instance.AssetsDirectory))
			{
				logger.LogError($"The specified game asset directory \"{instance.AssetsDirectory}\" does not exist or is inaccessible");
				result = null;
				return false;
			}

			if (instance.AssetListFile is not null && !File.Exists(instance.AssetListFile))
			{
				logger.LogError($"The specified asset list file \"{instance.AssetListFile}\" does not exist or is inacessible");
				result = null;
				return false;
			}

			result = instance;
			return true;
		}

		/// <summary>
		/// Prints how to use the program, including all possible command line arguments
		/// </summary>
		/// <param name="logger">Where the message will be printed</param>
		/// <param name="logLevel">The log level for the message</param>
		/// <param name="indent">Every line of the output will be prefixed with this</param>
		public static void PrintUsage(Logger logger, LogLevel logLevel, string indent = "")
		{
			string programName = Assembly.GetExecutingAssembly().GetName().Name ?? "Ue4Export";
			logger.Log(logLevel, $"{indent}Usage: {programName} [[options]] [game assets directory] [asset list file] [output directory]");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  [game assets directory]  Path to a directory containing game assets.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  [engine version]         The engine version the game was built with. See list below for values.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  [asset list file]        Path to text file with list of asset paths to export. See readme.md");
			logger.Log(logLevel, $"{indent}                           for more details.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  [output directory]       Directory to output exported assets.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}Options");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --mix-output       Do not clear the contents of the output directory before exporting.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --mappings [path]  The path to a usmap file for the game. This is necessary if the game contains");
			logger.Log(logLevel, $"{indent}                     unversioned data, such as a UE5 game. See readme for more information.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --key [key]        The AES encryption key for the game's data if the data is encrypted.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --no-subdirs       Do not search subdirectories for game assets. Only search the top level");
			logger.Log(logLevel, $"{indent}                     game assets directory.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --quiet            Minimal logging. Skips listing individual assets.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --silent           No logging unless an error occurs.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}Game engine versions");
			logger.Log(logLevel, $"{indent}  Pass in the engine version that best matches the game being dumped. If the game has a");
			logger.Log(logLevel, $"{indent}  specialized version, pass that in. Otherwise, pass in the engine version the game was");
			logger.Log(logLevel, $"{indent}  built with, which can be found in the properties of the game's exe.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  Following is a list of all possible engine version values.");
			logger.LogEmptyLine(logLevel);

			foreach (EGame version in Enum.GetValues<EGame>().ToHashSet())
			{
				string versionStr;
				switch (version)
				{
					case EGame.GAME_UE4_LATEST:
						versionStr = "UE4_LATEST";
						break;
					case EGame.GAME_UE5_LATEST:
						versionStr = "UE5_LATEST";
						break;
					default:
						versionStr = version.ToString()[5..]; // Trim GAME_
						break;
				}
				logger.Log(logLevel, $"  {versionStr}");
			}
		}

		/// <summary>
		/// Prints the current configuration of options
		/// </summary>
		/// <param name="logger">Where the message will be printed</param>
		/// <param name="logLevel">The log level for the message</param>
		/// <param name="indent">Every line of the output will be prefixed with this</param>
		public void PrintConfiguration(Logger logger, LogLevel logLevel, string indent = "")
		{
			string programName = Assembly.GetExecutingAssembly().GetName().Name ?? "Ue4Export";
			logger.Log(logLevel, $"{indent}{programName}");
			logger.Log(logLevel, $"{indent}  Asset directory   {AssetsDirectory}");
			logger.Log(logLevel, $"{indent}  Engine version    {EngineVersion.ToString()[5..]}");
			logger.Log(logLevel, $"{indent}  Asset list file   {AssetListFile}");
			logger.Log(logLevel, $"{indent}  Output directory  {OutputDirectory}");
			logger.Log(logLevel, $"{indent}  Mix output        {MixOutput}");
			logger.Log(logLevel, $"{indent}  Mappings path     {MappingsPath??"[None]"}");
			logger.Log(logLevel, $"{indent}  AES key           {(EncryptionKey is null ? "No" : "Yes")}");
		}
	}
}
