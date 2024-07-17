using System;
using System.IO;
using System.Reactive.Disposables;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.ReactiveUI;

using Newtonsoft.Json;

using ReactiveUI;

namespace Vk_Friends_Sender.Views;

public partial class MainWindow : ReactiveWindow<ViewModels.MainWindow> {
	public MainWindow() {
		InitializeComponent();
	}

	protected override async void OnLoaded(RoutedEventArgs e) {
#if RELEASE
		if (await StorageProvider.TryGetFileFromPathAsync(new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".state"))) is not {} file) {
			ViewModel = new() {
				Storage = StorageProvider
			};
		}
		else {
			using (var reader = new StreamReader(await file.OpenReadAsync())) {
				ViewModel = JsonConvert.DeserializeObject<ViewModels.MainWindow>(await reader.ReadToEndAsync());
			}

			if (ViewModel is null) {
				Logger.Sink.Log(LogEventLevel.Error, LogArea.Property, this, "Can't parse ViewModels from json");
				
				await file.DeleteAsync();

				ViewModel = new();
			}
			
			ViewModel.Storage = StorageProvider;
		}
#elif DEBUG
		ViewModel = new() {
			Storage = StorageProvider
		};
#endif
		
		
		this.WhenActivated(
			dispose => {
				// Properties binding
				this.OneWayBind(ViewModel, x => x.Proxies, x => x.list_Proxies.ItemsSource)
					.DisposeWith(dispose);

				this.OneWayBind(ViewModel, x => x.Tokens, x => x.list_Tokens.ItemsSource)
					.DisposeWith(dispose);

				this.Bind(
					ViewModel,
					x => x.UserId,
					x => x.box_UserId.Text,
					x => x == 0 ? "" : x.ToString(),
					x => long.TryParse(x, out var id) ? id : 0
				).DisposeWith(dispose);
				
				// Commands binding
				this.BindCommand(ViewModel, x => x.Proxies_Load, x => x.btn_ProxiesLoad)
					.DisposeWith(dispose);

				this.BindCommand(ViewModel, x => x.Proxies_Clear, x => x.btn_ProxiesClear)
					.DisposeWith(dispose);

				this.BindCommand(ViewModel, x => x.Cookies_Load, x => x.btn_TokensLoad)
					.DisposeWith(dispose);

				this.BindCommand(ViewModel, x => x.Cookies_Clear, x => x.btn_TokensClear)
					.DisposeWith(dispose);

				// Control Panel
				this.OneWayBind(ViewModel, x => x.CancelCanExecute, x => x.btn_Cancel.IsEnabled)
					.DisposeWith(dispose);
				
				this.BindCommand(ViewModel, x => x.Cancel, x => x.btn_Cancel)
					.DisposeWith(dispose);

				this.OneWayBind(ViewModel, x => x.SubmitCanExecute, x => x.btn_Submit.IsEnabled)
					.DisposeWith(dispose);
				
				this.BindCommand(ViewModel, x => x.Submit, x => x.btn_Submit)
					.DisposeWith(dispose);
			}
		);
		
		base.OnLoaded(e);
	}

	protected override void OnClosing(WindowClosingEventArgs e) {
#if RELEASE
		File.WriteAllText(".state", JsonConvert.SerializeObject(ViewModel));
#endif
		
		base.OnClosing(e);
	}
}