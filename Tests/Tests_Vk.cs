using Serilog;
using Serilog.Core;
using Serilog.Events;

using Vk_Friends_Sender.Services;

namespace Tests;

public class Tests_Vk {
	public async Task Test_SendRequest(string token, string user_agent, long user_id) {
		//
		var logger = new LoggerConfiguration()
					 .Enrich.FromLogContext()
					 .MinimumLevel.Verbose()
					 .WriteTo.Sink(new Logger())
					 .CreateLogger();
		
		var vk = new Vk("host:port:username:password", token, user_agent, new("2captcha api key"), logger);
		
		//
		await vk.AddToFriendsAsync(user_id);
		
		//
		vk.Dispose();
	}
	
	private class Logger : ILogEventSink {
		public void Emit(LogEvent logEvent) {
			TestContext.Out.WriteLine($"[{logEvent.Level}] {logEvent.RenderMessage()}");
		}
	}
}