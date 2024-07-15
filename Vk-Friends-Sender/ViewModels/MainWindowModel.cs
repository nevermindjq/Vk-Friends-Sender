using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

using Avalonia.Platform.Storage;

using ReactiveUI;

using Vk_Friends_Sender.Models;

namespace Vk_Friends_Sender.ViewModels {
	public class MainWindowModel : ReactiveObject {
		public ObservableCollection<Proxy> Proxies { get; } = [
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
			"host:0:username:password",
		];

		public required IStorageProvider Storage { get; init; }

		// host:port:username:password
		public ICommand Proxies_Load => ReactiveCommand.CreateFromTask(
			async () => {
				// Pick file
				var options = new FilePickerOpenOptions {
					AllowMultiple = false,
					FileTypeFilter = [
						FilePickerFileTypes.TextPlain
					],
					Title = "Cookies picker"
				};

				if (await Storage.OpenFilePickerAsync(options) is not {Count: > 0} files) {
					return;
				}
				
				var file = files.First();

				// Process file

				using (var stream = await file.OpenReadAsync()) {
					using (var reader = new StreamReader(stream)) {
						while (!reader.EndOfStream) {
							Proxies.Add(await reader.ReadLineAsync());
						}
					}
				}
			}
		);
		
		public ICommand Proxies_Check { get; }
		public ICommand Proxies_Clear { get; }
	}
}