using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.OutboundHandlers
{
	public class XmlApiSender
	{
		private static readonly string apiUrl = "https://mule-rtf-test.levi.com/retail-pos-ph-rpp-exp-api-dev1/retail-pos-ph-rpp-exp-api/v1/sale";
		private static readonly string username = "1d75a7f3-1b67-4c6e-9c6e-d0f6ba114417";
		private static readonly string password = "3~E8Q~CKgCliOmXmKjSVXJtrffHYED4_cKDPhax4";

		public static async Task<bool> SendXmlAsync(string rawXml)
		{
			using (HttpClient client = new HttpClient())
			{
				try
				{
					// Set authentication
					var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

					// Wrap XML inside SOAP envelope
					string soapEnvelope = $@"
<?xml version=""1.0"" ?>
<S:Envelope xmlns:S=""http://schemas.xmlsoap.org/soap/envelope/"">
    <S:Body>
        <ns2:postTransaction xmlns:ns2=""http://v1.ws.poslog.xcenter.dtv/"">
            <rawPoslogString>{System.Security.SecurityElement.Escape(rawXml)}</rawPoslogString>
        </ns2:postTransaction>
    </S:Body>
</S:Envelope>";

					var content = new StringContent(soapEnvelope, Encoding.UTF8, "application/xml");

					// Send request
					HttpResponseMessage response = await client.PostAsync(apiUrl, content);

					if (response.IsSuccessStatusCode)
					{
						Console.WriteLine("✅ XML posted successfully.");
						return true;
					}
					else
					{
						string error = await response.Content.ReadAsStringAsync();
						Console.WriteLine("❌ Failed to post XML: " + response.StatusCode + "\n" + error);
						return false;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("❌ Exception during API call: " + ex.Message);
					return false;
				}
			}
		}
	}
}
