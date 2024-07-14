using Avalonia;

using System;

using Avalonia.ReactiveUI;

namespace Vk_Friends_Sender;

class Program {
	[STAThread]
	public static void Main(string[] args) => BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
					 .UseReactiveUI()
					 .UsePlatformDetect()
					 .WithInterFont()
					 .LogToTrace();
}