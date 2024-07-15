using Avalonia;
using Avalonia.Logging;

using Serilog;

using Vk_Friends_Sender.Logging;

namespace Vk_Friends_Sender {
	internal static class Extensions {
		public static AppBuilder LogToSerilog(this AppBuilder app, LogEventLevel level = LogEventLevel.Verbose, params string[] areas) {
			Log.Logger = new LoggerConfiguration()
						 .Enrich.FromLogContext()
						 .MinimumLevel.Is((Serilog.Events.LogEventLevel)level)
						 .WriteTo.Console()
						 .WriteTo.File(".log", fileSizeLimitBytes: 1024 * 1024 * 20)
						 .CreateLogger();
			
			Logger.Sink = new SerilogSink(level, areas.Length == 0 ? [
				LogArea.Visual,
				LogArea.Platform,
				LogArea.Animations,
				LogArea.Binding,
				LogArea.Control,
				LogArea.Layout,
				LogArea.Property,
				
				// Platforms
				LogArea.Win32Platform,
				LogArea.LinuxFramebufferPlatform
			] : areas);
            
			return app;
		}
	}
}