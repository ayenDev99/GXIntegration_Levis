using System;
using System.IO;

namespace GXIntegration_Levis.Helpers
{
	public static class Logger
	{
		private static readonly object _lock = new object();

		// Event for log messages
		public static event Action<string> OnLogMessage;

		public static void Log(string message, bool isAuto = false)
		{
			try
			{
				string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
				Directory.CreateDirectory(logDir);

				string logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
				string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				string logMessage = $"[{timestamp}] {message}";

				Console.WriteLine(logMessage);
				
				if(!isAuto)
				{
					// Raise event so subscribers can react (like ProgressForm)
					OnLogMessage?.Invoke(logMessage);
				}

				lock (_lock)
				{
					using (var stream = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
					using (var writer = new StreamWriter(stream))
					{
						writer.WriteLine(logMessage);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Logger error: {ex.Message}");
			}
		}
	}

}
