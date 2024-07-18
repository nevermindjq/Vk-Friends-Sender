using System;
using System.Linq;
using System.Net;

namespace Vk_Friends_Sender.Models {
	public class Proxy {
		public string Host { get; set; } = "Host";
		public int Port { get; set; }
		public string Username { get; set; } = "Username";
		public string Password { get; set; } = "Password";

		public static implicit operator Proxy(string str) {
			var data = str.Split('@')
						  .SelectMany(x => x.Split(':'))
						  .ToArray();

			if (data.Length != 4) {
				throw new ArgumentNullException($"Proxy has no credentials or empty: {str}", nameof(str));
			}

			int host_index = -1;

			for (int i = 0; i < data.Length; i++) {
				if (data[i].Split('.').Length == 4) {
					host_index = i;
					break;
				}
			}

			var username_index = host_index switch {
				0 => 2,
				2 => 0,
				_ => throw new ArgumentException("Proxy's host not found")
			};

			return new() {
				Host = data[host_index],
				Port = int.Parse(data[host_index + 1]),
				Username = data[username_index],
				Password = data[username_index + 1]
			};
		}

		public static implicit operator WebProxy(Proxy proxy) => new(new Uri($"http://{proxy.Host}:{proxy.Port}")) {
			Credentials = new NetworkCredential(proxy.Username, proxy.Password)
		};

		public static implicit operator Microsoft.Playwright.Proxy(Proxy proxy) => new() {
			Server = $"http://{proxy.Host}:{proxy.Port}",
			Username = proxy.Username,
			Password = proxy.Password
		};

		public static implicit operator string(Proxy proxy) => $"{proxy.Username}:{proxy.Password}@{proxy.Host}:{proxy.Port}";
	}
}