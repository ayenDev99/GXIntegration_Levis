using GXIntegration;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

class Program
{
	static GXConfig config;
	static Form1 form;
	static readonly CancellationTokenSource cts = new CancellationTokenSource();

	public static string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

	[STAThread]
	static async Task Main()
	{
		string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
		config = GXConfig.Load(configPath);

		// Start WinForms UI
		Thread formThread = new Thread(() =>
		{
			form = new GXIntegration.Form1();

			form.Load += async (sender, e) =>
			{
				Task.Run(() => RunAutoOutboundAPIAsync(cts.Token));
				Task.Run(() => RunAutoOutboundEODAsync(cts.Token));
			};

			form.FormClosed += (s, e) => cts.Cancel();

			Application.Run(form);
		});

		formThread.SetApartmentState(ApartmentState.STA);
		formThread.Start();

		try
		{
			await Task.Delay(Timeout.Infinite, cts.Token); // keep main alive
		}
		catch (TaskCanceledException)
		{
			// expected on shutdown
		}
	}

	// ------------------------------
	// AUTO OUTBOUND - API (interval)
	// ------------------------------
	static async Task RunAutoOutboundAPIAsync(CancellationToken token)
	{
		int iteration = 0;
		int processInterval = config.OutApiAutoProcessTime; // in minutes

		while (!token.IsCancellationRequested)
		{
			iteration++;
			try
			{
				Logger.Log("**************************************************************************");
				Logger.Log($">>> [AUTO OUTBOUND - API] START iteration {iteration} at {CurrentTime}");
				Logger.Log("**************************************************************************");
				Logger.Log($">>> Interval = {processInterval} minute(s)");

				await form.OutboundAPITab.TriggerAPIAsync(processInterval);
			}
			catch (Exception ex)
			{
				Logger.Log("ERROR (API): " + ex);
			}

			Logger.Log($">>> Waiting {processInterval} minute(s) before next API run...");
			try
			{
				await Task.Delay(TimeSpan.FromMinutes(processInterval), token);
			}
			catch (TaskCanceledException)
			{
				break;
			}
		}
	}

	// ------------------------------
	// AUTO OUTBOUND - EOD (daily time)
	// ------------------------------
	static async Task RunAutoOutboundEODAsync(CancellationToken token)
	{
		if (!TimeSpan.TryParse(config.OutEodAutoProcessTime, out TimeSpan scheduledTime))
		{
			Logger.Log("ERROR: Invalid OutEodAutoProcessTime in config.xml. Expected format HH:mm:ss");
			return;
		}

		int iteration = 0;

		while (!token.IsCancellationRequested)
		{
			DateTime now = DateTime.Now;
			DateTime nextRun = now.Date.Add(scheduledTime);

			// If the scheduled time has already passed today, schedule for tomorrow
			if (nextRun <= now)
				nextRun = nextRun.AddDays(1);

			TimeSpan delay = nextRun - now;

			Logger.Log("**************************************************************************");
			Logger.Log($">>> [AUTO OUTBOUND - EOD] Scheduled run at {nextRun:yyyy-MM-dd HH:mm:ss}");
			Logger.Log("**************************************************************************");

			try
			{
				await Task.Delay(delay, token); // wait until the scheduled time
				if (token.IsCancellationRequested) break;

				iteration++;
				Logger.Log($">>> [AUTO OUTBOUND - EOD] START iteration {iteration} at {CurrentTime}");

				// ✅ Pass the scheduled time string (e.g., "11:30:00")
				await form.OutboundEODTab.TriggerEODAsync(config.OutEodAutoProcessTime);

				Logger.Log($">>> [AUTO OUTBOUND - EOD] Completed iteration {iteration}");
			}
			catch (TaskCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				Logger.Log("ERROR (EOD): " + ex);
			}
		}
	}

}
