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

namespace Ue4Export
{
	/// <summary>
	/// Utility for logging output messages
	/// </summary>
	internal class Logger
	{
		private readonly TextWriter[] mWriters;

		/// <summary>
		/// The minimum level of messages to print. Logged messages below this threshold will be discarded
		/// </summary>
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

		/// <summary>
		/// Sets the output device the logger will use for a specific log level.
		/// </summary>
		public void SetOutput(LogLevel level, TextWriter output)
		{
			mWriters[(int)level] = output;
		}


		/// <summary>
		/// Logs a message at a specific level
		/// </summary>
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

		/// <summary>
		/// Logs a completely empty line at a specific level
		/// </summary>
		public void LogEmptyLine(LogLevel level)
		{
			if (level < LogLevel) return;

			OnPreLog(level, string.Empty);

			mWriters[(int)level].WriteLine(string.Empty);

			OnPostLog(level, string.Empty);
		}

		/// <summary>
		/// Helper for logging an error
		/// </summary>
		public void LogError(string message)
		{
			Log(LogLevel.Error, message);
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
		private readonly ConsoleColor mOriginalColor;

		public ConsoleLogger() : base(Console.Out)
		{
			mOriginalColor = Console.ForegroundColor;

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

		protected override void OnPostLog(LogLevel level, string message)
		{
			Console.ForegroundColor = mOriginalColor;
		}
	}

	/// <summary>
	/// Represents the importance of messages being logged
	/// </summary>
	internal enum LogLevel : int
	{
		/// <summary>
		/// For spam messages that will not be printed by default
		/// </summary>
		Verbose,
		/// <summary>
		/// For debugging messages that will only print ina  debug build by default
		/// </summary>
		Debug,
		/// <summary>
		/// For informational messages. Will print by default
		/// </summary>
		Information,
		/// <summary>
		/// For important informational messages. Will print by default
		/// </summary>
		Important,
		/// <summary>
		/// For warnings. Will print by default
		/// </summary>
		Warning,
		/// <summary>
		/// For errors. Will print by default
		/// </summary>
		Error,
		/// <summary>
		/// For fatal errors. Will always print
		/// </summary>
		Fatal
	}
}
