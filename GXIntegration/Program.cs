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
	static void Main()
	{
		var thread = new Thread(() =>
		{
			MainAsync().GetAwaiter().GetResult();
		});

		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
	}

	static GXConfig config;
	static Form1 form;
	public static string CurrentTime => DateTime.Now.ToString("HH:mm");

	static async Task MainAsync()
	{
		string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
		config = GXConfig.Load(configPath);

		var tcsApp = new TaskCompletionSource<bool>();

		var formThread = new Thread(() =>
		{
			form = new GXIntegration.Form1();

			form.Load += async (sender, e) =>
			{
				await RunIterationLoop();
			};

			Application.Run(form);
			tcsApp.SetResult(true);
		});

		formThread.SetApartmentState(ApartmentState.STA);
		formThread.Start();

		await tcsApp.Task;
	}

	static async Task RunIterationLoop()
	{
		int iteration = 0;

		while (true)
		{
			iteration++;
			try
			{
				Logger.Log("**************************************************************************");
				Logger.Log($">>> START iteration {iteration} at {CurrentTime}");
				Logger.Log("**************************************************************************");

				Logger.Log($">>> ReprocessMinutes = {config.ReprocessMinutes}");

				// Condition: INBOUND is triggered every [config.processMinutes] minutes. Process if the SFTP folder has files.
				// Condition: OUTBOUND EOD is triggered every [config.morningProcess, config.eveningProcess]. Process as EOD.
				// Condition: OUTBOUND API is triggered every [config.processMinutes] minutes and check if prism has transaction within the range.

				//Logger.Log(">>> Starting OUTBOUND Process...");
				//await form.OutboundTab.TriggerDownloadAsync();
				//Logger.Log(">>> OUTBOUND completed");

				await form.OutboundAPITab.TriggerAPIAsync();
			}
			catch (Exception ex)
			{
				Logger.Log("ERROR : " + ex.ToString());
			}

			Logger.Log($">>> Waiting {config.ReprocessMinutes} minute(s) before next iteration...");
			await Task.Delay(TimeSpan.FromMinutes(config.ReprocessMinutes));
		}
	}


}
