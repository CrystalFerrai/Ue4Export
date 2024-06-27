using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ue4Export
{
	internal class Options
	{
		public string PaksDirectory { get; set; }

		public string OutputDirectory { get; set; }

		public string AssetListFile { get; set; }

		public string? EncryptionKey { get; set; }

		private Options()
		{
			PaksDirectory = null!;
			OutputDirectory = null!;
			AssetListFile = null!;
			EncryptionKey = null;
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
		/// <param name="indent">Every line of the output will be prefixed with this</param>
		public static void PrintUsage(Logger logger, string indent = "")
		{
			string? programName = Assembly.GetExecutingAssembly().GetName().Name;
			logger.Log(LogLevel.Important, $"{indent}Usage: {programName} [[options]] [game assets directory] [asset list file] [output directory]");
			logger.LogEmptyLine(LogLevel.Important);
			logger.Log(LogLevel.Important, $"{indent}  [game assets directory]  Path to a directory containing .pak files for a game.");
			logger.LogEmptyLine(LogLevel.Important);
			logger.Log(LogLevel.Important, $"{indent}  [asset list file]        Path to text file with list of asset paths to export. See readme.md for more details.");
			logger.LogEmptyLine(LogLevel.Important);
			logger.Log(LogLevel.Important, $"{indent}  [output directory]       Directory to output exported assets.");
			logger.LogEmptyLine(LogLevel.Important);
			logger.Log(LogLevel.Important, $"{indent}Options");
			logger.LogEmptyLine(LogLevel.Important);
			logger.Log(LogLevel.Important, $"{indent}  --key       The AES encryption key for the game's data if the data is encrypted.");
		}
	}
}
