using System;
using System.Net;

namespace Vk_Friends_Sender.Models {
	public class Proxy {
		public string Host { get; set; } = "Host";
		public int Port { get; set; }
		public string Username { get; set; } = "Username";
		public string Password { get; set; } = "Password";

		public static implicit operator Proxy(string str) {
			var data = str.Split(':');

			return new() {
				Host = data[0],
				Port = int.Parse(data[1]),
				Username = data[2],
				Password = data[3]
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