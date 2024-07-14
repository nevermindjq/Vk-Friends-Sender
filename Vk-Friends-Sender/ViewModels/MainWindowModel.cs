using System.Collections.ObjectModel;
using System.Windows.Input;

using ReactiveUI;

namespace Vk_Friends_Sender.ViewModels {
	public class MainWindowModel : ReactiveObject {
		public ObservableCollection<string> Proxies { get; } = [
			"Some proxy 1",
			"Some proxy 2"
		]; // TODO create and replace 'string' type with 'Proxy' type
		
		// TODO initialize commands
		public ICommand Load { get; }
		public ICommand Check { get; }
		public ICommand Clear { get; }
	}
}