using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Playwright;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog;

using TwoCaptcha.Captcha;

using Vk_Friends_Sender.Exceptions;

using Proxy = Vk_Friends_Sender.Models.Proxy;

namespace Vk_Friends_Sender.Services {
	public class Vk : IDisposable {
		private readonly string _token;
		private readonly Proxy _proxy;
		private readonly string? _user_agent;
		private readonly HttpClient _http;
		private readonly ILogger? _logger;
		private readonly TwoCaptcha.TwoCaptcha? _solver;

		public Vk(Proxy proxy, string token, string? user_agent = null, TwoCaptcha.TwoCaptcha? solver = null, ILogger? logger = null) {

			_proxy = proxy;
			_token = token;
			
			_user_agent = user_agent;
			_solver = solver;
			_logger = logger;
			
			//
			var handler = new HttpClientHandler {
				Proxy = (WebProxy)proxy
			};

			_http = new(handler, true);
		}

		#region IDisposable

		public void Dispose() {
			_http.Dispose();
		}

		#endregion
		
		public async Task AddToFriendsAsync(long user_id) {
			if (_token is null) {
				throw new("Account is not authorized");
			}

			var request = new HttpRequestMessage(HttpMethod.Post, "https://api.vk.com/method/friends.add?v=5.199&client_id=6287487") {
				Content = new MultipartFormDataContent {
					{ new StringContent(_token), "access_token" },
					{ new StringContent(user_id.ToString()), "user_id" }
				}
			};

			if (_user_agent is not null && !request.Headers.TryAddWithoutValidation("User-Agent", _user_agent)) {
				throw new("[VK API] Error while setting 'User-Agent': " + _user_agent);
			}

			if (await _http.SendAsync(request) is not { } response) {
				throw new("[VK API] Error null reference of response");
			}

			var content = await response.Content.ReadAsStringAsync();

			if (content.Contains("error") && !await _TrySolveErrorAsync(content)) {
				throw new($"[VK API] Error while adding to friends: {await response.Content.ReadAsStringAsync()}");
			}

#if DEBUG
			_logger?.Debug("[VK API] Add to friends successfully executed");
#endif
		}

		private Task<bool> _TrySolveErrorAsync(string content) {
			var json = JsonConvert.DeserializeObject<JObject>(content)!.Value<JObject>("error");

			switch (json.Value<int>("error_code")) {
				case 17:
					if (_solver is null) {
						throw new NullReferenceException("[VK API] TwoCaptcha solver not setted");
					}
					
					return _SolveCaptchaAsync(json.Value<string>("redirect_uri"));
				case 1117:
					throw new TokenExpiredException(_token);
			}

			return Task.FromResult(false);
		}

		private async Task<bool> _SolveCaptchaAsync(string url) {
			//
			url = url.Replace("validate", "captcha");

			using var playwright = await Playwright.CreateAsync();
			
			await using var browser = await playwright.Chromium.LaunchAsync(
				new() {
					Proxy = _proxy
				}
			);

			var page = await browser.NewPageAsync();

			await page.GotoAsync(url);
			
			//
			var src = await page.GetAttributeAsync("img.captcha_img", "src");

			if (src is null) {
				throw new("[VK API] Error while solving captcha: captcha img src not found");
			}

			var code = await _SolveWith2Captcha(src);

			if (code is null) {
				return false;
			}

			await page.FillAsync("input[name='captcha_key']", code);
			await page.ClickAsync("input[type='submit']");

			return true;
		}

		private async Task<string?> _SolveWith2Captcha(string src) {
			var captcha = new Normal();
			
			captcha.SetBase64(Convert.ToBase64String(await _LoadCaptchaImageAsync(src)));
			captcha.SetPhrase(false);
			captcha.SetCalc(false);
			captcha.SetCaseSensitive(false);
			captcha.SetHintText("Type text from the image");
			captcha.SetMinLen(4);
			captcha.SetMaxLen(6);
			captcha.SetLang("en");

			try {
				await _solver.Solve(captcha);
			} 
			catch (Exception e) {
				_logger?.Warning(e, "[VK API] Captcha not solved: {src}", src);

				return null;
			}

			return captcha.Code;
		}

		private async Task<byte[]> _LoadCaptchaImageAsync(string src) {
			var buffer = new byte[1024 * 3];
			var received = 0;
			
			using (var source = await _http.GetStreamAsync(src)) {
				received = await source.ReadAsync(buffer, 0, buffer.Length);
			}

			return buffer[..received];
		}
	}
}