using System.Reactive.Disposables;

using Avalonia.ReactiveUI;

using ReactiveUI;

namespace Vk_Friends_Sender.Views;

public partial class MainWindow : ReactiveWindow<ViewModels.MainWindow> {
	public MainWindow() {
		InitializeComponent();

		ViewModel = new() {
			Storage = StorageProvider
		};
		
		this.WhenActivated(
			dispose => {
				// Properties binding
				this.OneWayBind(ViewModel, x => x.Proxies, x => x.list_Proxies.ItemsSource)
					.DisposeWith(dispose);

				this.OneWayBind(ViewModel, x => x.Cookies, x => x.list_Cookies.ItemsSource)
					.DisposeWith(dispose);
				
				// Commands binding
				this.BindCommand(ViewModel, x => x.Proxies_Load, x => x.btn_ProxiesLoad)
					.DisposeWith(dispose);

				this.BindCommand(ViewModel, x => x.Proxies_Clear, x => x.btn_ProxiesClear)
					.DisposeWith(dispose);

				this.BindCommand(ViewModel, x => x.Cookies_Load, x => x.btn_CookiesLoad)
					.DisposeWith(dispose);

				this.BindCommand(ViewModel, x => x.Cookies_Clear, x => x.btn_CookiesClear)
					.DisposeWith(dispose);
			}
		);
	}
}