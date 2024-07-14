using Avalonia.ReactiveUI;

using Vk_Friends_Sender.ViewModels;

namespace Vk_Friends_Sender.Views;

public partial class MainWindow : ReactiveWindow<MainWindowModel> {
	public MainWindow() {
		InitializeComponent();
	}
}