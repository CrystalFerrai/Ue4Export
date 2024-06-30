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

using CUE4Parse.Compression;

namespace Ue4Export
{
	internal class Program
	{
		static int Main(string[] args)
		{
			Logger logger = new ConsoleLogger();

			if (args.Length == 0)
			{
				Options.PrintUsage(logger, LogLevel.Important);
				return OnExit(0);
			}

			Options? options;
			if (!Options.TryParseCommandLine(args, logger, out options))
			{
				logger.LogEmptyLine(LogLevel.Information);
				Options.PrintUsage(logger, LogLevel.Important);
				return OnExit(1);
			}

			// Default log level is Information in release builds and Debug in debug builds
			if (options.IsSilent)
			{
				logger.LogLevel = LogLevel.Error;
			}
			else if (options.IsQuiet)
			{
				logger.LogLevel = LogLevel.Important;
			}

			options.PrintConfiguration(logger, LogLevel.Information);
			logger.LogEmptyLine(LogLevel.Information);

			try
			{
				Directory.CreateDirectory(options.OutputDirectory);
			}
			catch (Exception ex)
			{
				logger.Log(LogLevel.Fatal, $"Could not access/create output directory \"{options.OutputDirectory}\". [{ex.GetType().FullName}] {ex.Message}");
				return OnExit(1);
			}

			if (!options.MixOutput)
			{
				try
				{
					if (Directory.Exists(options.OutputDirectory))
					{
						logger.Log(LogLevel.Important, "Clearing output directory...");
						DeleteDirectoryContents(options.OutputDirectory);
					}
				}
				catch (Exception ex)
				{
					logger.Log(LogLevel.Fatal, $"Could not clear output directory \"{options.OutputDirectory}\". [{ex.GetType().FullName}] {ex.Message}");
					return OnExit(1);
				}
			}

			ZlibHelper.Initialize(ZlibHelper.DLL_NAME);
			OodleHelper.Initialize(OodleHelper.OODLE_DLL_NAME);

			logger.Log(LogLevel.Important, "Exporting assets...");
			Exporter exporter = new Exporter(options, logger);
			bool success = exporter.Export();

			if (!success)
			{
				logger.Log(LogLevel.Warning, "One or more assets failed to export.");
			}

			logger.Log(LogLevel.Important, "\nExports complete.");

			return OnExit(success ? 0 : 2);
		}

		private static void DeleteDirectoryContents(string path)
		{
			DirectoryInfo directory = new DirectoryInfo(path);

			foreach (FileInfo file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
			{
				file.Delete();
			}

			foreach (DirectoryInfo dir in directory.EnumerateDirectories("*", SearchOption.AllDirectories).Reverse())
			{
				dir.Delete();
			}
		}

		private static int OnExit(int code)
		{
			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.Out.WriteLine("Press a key to exit");
				Console.ReadKey();
			}
			return code;
		}
	}
}