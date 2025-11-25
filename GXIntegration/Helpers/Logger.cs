using System;
using System.IO;

namespace GXIntegration_Levis.Helpers
{
	public static class Logger
	{
		private static readonly object _lock = new object();

		public static event Action<string> OnLogMessage;

		public enum LogType
		{
			Inbound,
			Outbound,
			Error
		}

		// ============================
		// PUBLIC SHORTCUT METHODS
		// ============================
		public static void LogInbound(string message, bool isAuto = false)
			=> Log(message, LogType.Inbound, isAuto);

		public static void LogOutbound(string message, bool isAuto = false)
			=> Log(message, LogType.Outbound, isAuto);

		public static void LogError(string message, bool isAuto = false)
			=> Log(message, LogType.Error, isAuto);

		// ============================
		// MAIN LOG METHOD
		// ============================
		public static void Log(string message, LogType logType, bool isAuto = false)
		{
			try
			{
				string logDir = Path.Combine(
					AppDomain.CurrentDomain.BaseDirectory,
					"logs",
					logType.ToString()
				);

				EnsureDirectoryExists(logDir);

				string logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");

				string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				string logMessage = $"[{timestamp}] [{logType}] {message}";

				Console.WriteLine(logMessage);

				if (!isAuto)
					OnLogMessage?.Invoke(logMessage);

				lock (_lock)
				{
					File.AppendAllText(logFile, logMessage + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Logger error: {ex.Message}");
			}
		}

		private static void EnsureDirectoryExists(string path)
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}
	}
}
