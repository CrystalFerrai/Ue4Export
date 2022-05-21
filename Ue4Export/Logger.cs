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

namespace Ue4Export
{
	/// <summary>
	/// Utility for logging output messages
	/// </summary>
	internal class Logger
	{
		private readonly TextWriter[] mWriters;

		public LogLevel LogLevel { get; set; }

		public Logger(TextWriter defaultOutput)
		{
			mWriters = new TextWriter[Enum.GetNames<LogLevel>().Length];
			for (int i = 0; i < mWriters.Length; ++i)
			{
				mWriters[i] = defaultOutput;
			}

#if DEBUG
			LogLevel = LogLevel.Debug;
#else
			LogLevel = LogLevel.Information;
#endif
		}

		public void SetOutput(LogLevel level, TextWriter output)
		{
			mWriters[(int)level] = output;
		}

		public void Log(LogLevel level, string message)
		{
			if (level < LogLevel) return;

			OnPreLog(level, message);

			if (level == LogLevel.Warning)
			{
				mWriters[(int)level].WriteLine($"[WARNING] {message}");
			}
			else if (level >= LogLevel.Error)
			{
				mWriters[(int)level].WriteLine($"[ERROR] {message}");
			}
			else
			{
				mWriters[(int)level].WriteLine(message);
			}

			OnPostLog(level, message);
		}

		protected virtual void OnPreLog(LogLevel level, string message)
		{
		}

		protected virtual void OnPostLog(LogLevel level, string message)
		{
		}
	}

	/// <summary>
	/// Logger designed to log to the console
	/// </summary>
	internal class ConsoleLogger : Logger
	{
		public ConsoleLogger() : base(Console.Out)
		{
			SetOutput(LogLevel.Error, Console.Error);
			SetOutput(LogLevel.Fatal, Console.Error);
		}

		protected override void OnPreLog(LogLevel level, string message)
		{
			switch (level)
			{
				case LogLevel.Verbose:
				case LogLevel.Debug:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
				case LogLevel.Information:
					Console.ForegroundColor = ConsoleColor.Gray;
					break;
				case LogLevel.Important:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case LogLevel.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case LogLevel.Error:
				case LogLevel.Fatal:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
			}
		}
	}

	internal enum LogLevel : int
	{
		Verbose,
		Debug,
		Information,
		Important,
		Warning,
		Error,
		Fatal
	}
}
