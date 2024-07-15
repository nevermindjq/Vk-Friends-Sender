using System.Reactive.Disposables;

using Avalonia.ReactiveUI;

using ReactiveUI;

using Vk_Friends_Sender.ViewModels;

namespace Vk_Friends_Sender.Views;

public partial class MainWindow : ReactiveWindow<MainWindowModel> {
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
				
				// Commands binding
				this.BindCommand(ViewModel, x => x.Proxies_Load, x => x.btn_ProxiesLoad)
					.DisposeWith(dispose);
			}
		);
	}
}