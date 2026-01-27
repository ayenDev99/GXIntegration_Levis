using GXIntegration;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.InboundHandlers;
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
            var _ = typeof(Guna.UI2.WinForms.Guna2Panel);

            form = new GXIntegration.Form1();

			Logger.LogOutbound($"---- APPLICATION STARTED", true);
			Logger.LogInbound($"---- APPLICATION STARTED", true);

			form.Load += async (sender, e) =>
			{
				Task.Run(() => RunAutoOutboundEODAsync(cts.Token));
				Task.Run(() => RunAutoOutboundAPIAsync(cts.Token));
				Task.Run(() => RunAutoInboundDownloadSFTPAsync(cts.Token));
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
        if (form == null)
        {
            throw new Exception("Form is null!");
        }

        if (form.OutboundAPITab == null)
        {
            throw new Exception("OutboundAPITab is null!");
        }
		int processInterval = config.OutApiAutoProcessTime; // in minutes

        // Safe to call
        await form.OutboundAPITab.TriggerAPIAsync(processInterval);

        int iteration = 0;
		var is_auto = true;

		Logger.LogOutbound($"[AUTO - API] Interval = {processInterval} minute(s)", is_auto);

		while (!token.IsCancellationRequested)
		{
			iteration++;
			try
			{
				await form.OutboundAPITab.TriggerAPIAsync(processInterval);
			}
			catch (Exception ex)
			{
				Logger.LogError($"ERROR (API): {ex}", is_auto);
			}

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
		var is_auto = true;

		if (!TimeSpan.TryParse(config.OutEodAutoProcessTime, out TimeSpan scheduledTime))
		{
			Logger.LogError("ERROR: Invalid OutEodAutoProcessTime in config.xml. Expected format HH:mm:ss", is_auto);
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

			Logger.LogOutbound($"[AUTO - EOD] Scheduled run at {nextRun:yyyy-MM-dd HH:mm:ss}", is_auto);

			try
			{
				await Task.Delay(delay, token); // wait until the scheduled time
				if (token.IsCancellationRequested) break;

				iteration++;
				Logger.LogOutbound($"[AUTO - EOD] START iteration {iteration} at {CurrentTime}", is_auto);

				await form.OutboundEODTab.TriggerEODAsync(config.OutEodAutoProcessTime);

				Logger.LogOutbound($"[AUTO - EOD] Completed iteration {iteration}", is_auto);
			}
			catch (TaskCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				Logger.LogError($"ERROR (EOD): {ex}", is_auto);
			}
		}
	}

	// ------------------------------
	// AUTO INBOUND - SFTP (interval)
	// ------------------------------
	static async Task RunAutoInboundDownloadSFTPAsync(CancellationToken token)
	{

		int iteration = 0;
		int processInterval = config.InAutoDownloadProcessTime;
		bool isAuto = true;

		Logger.LogInbound($"[AUTO - SFTP] Interval = {processInterval} minute(s)", isAuto);

		var globalInbound = new GlobalInbound();
		// Block async so BackgroundWorker waits
		string session = globalInbound.AuthenticateFromConfigAsync()
										.GetAwaiter()
										.GetResult();

        if (form == null)
            throw new Exception("Form instance is null!");

        if (form.InboundPage == null)
            throw new Exception("InboundPage is null!");

        if (session == null)
            throw new Exception("Session object is null!");


        while (!token.IsCancellationRequested)
		{
			iteration++;
			try
			{
				await form.InboundPage.TriggerSFTPAsync(processInterval, session);
			}
			catch (Exception ex)
			{
				Logger.LogError("ERROR (SFTP): " + ex);
			}

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

}
