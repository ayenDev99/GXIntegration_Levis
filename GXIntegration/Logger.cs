using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis
{
	public static class Logger
	{
		public static void Log(string message)
		{
			string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
			Directory.CreateDirectory(logDir);

			string logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			string logMessage = $"[{timestamp}] {message}";

			Console.WriteLine(logMessage);
			File.AppendAllText(logFile, logMessage + Environment.NewLine);
		}
	}
}
