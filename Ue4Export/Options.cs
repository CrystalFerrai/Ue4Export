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
		/// The directory containing pak files to exported assets from
		/// </summary>
		public string PaksDirectory { get; set; }

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
		/// The encryption key to use when accessing pak files
		/// </summary>
		public string? EncryptionKey { get; set; }

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
			PaksDirectory = null!;
			OutputDirectory = null!;
			AssetListFile = null!;
			MixOutput = false;
			EncryptionKey = null;
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
							instance.PaksDirectory = Path.GetFullPath(args[i]);
							break;
						case 1:
							instance.AssetListFile = Path.GetFullPath(args[i]);
							break;
						case 2:
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

			if (positionalArgIndex < 3)
			{
				logger.LogError($"Not enough positional arguments");
				result = null;
				return false;
			}

			if (!Directory.Exists(instance.PaksDirectory))
			{
				logger.LogError($"The specified game asset directory \"{instance.PaksDirectory}\" does not exist or is inaccessible");
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
			logger.Log(logLevel, $"{indent}  [game assets directory]  Path to a directory containing .pak files for a game.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  [asset list file]        Path to text file with list of asset paths to export. See readme.md for more details.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  [output directory]       Directory to output exported assets.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}Options");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --mix-output  Do not clear the contents of the output directory before exporting.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --key [key]   The AES encryption key for the game's data if the data is encrypted.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --quiet       Minimal logging. Skips listing individual assets.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --silent      No logging unless an error occurs.");
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
			logger.Log(logLevel, $"{indent}  Asset directory   {PaksDirectory}");
			logger.Log(logLevel, $"{indent}  Asset list file   {AssetListFile}");
			logger.Log(logLevel, $"{indent}  Output directory  {OutputDirectory}");
			logger.Log(logLevel, $"{indent}  Mix Output        {MixOutput}");
			logger.Log(logLevel, $"{indent}  AES Key           {(EncryptionKey is null ? "No" : "Yes")}");
		}
	}
}
