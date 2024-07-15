using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

using Avalonia.Platform.Storage;

using ReactiveUI;

using Vk_Friends_Sender.Models;

namespace Vk_Friends_Sender.ViewModels {
	public class MainWindow : ReactiveObject {
		public ObservableCollection<Proxy> Proxies { get; } = [
#if DEBUG
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
#endif
		];

		public ObservableCollection<Cookie> Cookies { get; } = [
#if DEBUG
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
			new(),
#endif
		];

		// External properties
		public required IStorageProvider Storage { get; init; }

		#region Proxies

		// host:port:username:password
		public ICommand Proxies_Load => ReactiveCommand.CreateFromTask(
			async () => {
				// Pick file
				var options = new FilePickerOpenOptions {
					AllowMultiple = false,
					FileTypeFilter = [
						FilePickerFileTypes.TextPlain
					],
					Title = "Proxies picker"
				};

				if (await Storage.OpenFilePickerAsync(options) is not {Count: > 0} files || files.FirstOrDefault() is not {} file) {
					return;
				}

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

		public ICommand Proxies_Clear => ReactiveCommand.Create(() => Proxies.Clear());

		#endregion

		#region Cookies

		public ICommand Cookies_Load => ReactiveCommand.CreateFromTask(
			async () => {
				// Pick folder 
				var options = new FolderPickerOpenOptions {
					AllowMultiple = false,
					Title = "Cookies folder picker"
				};

				if (await Storage.OpenFolderPickerAsync(options) is not {Count: > 0} folders || folders.FirstOrDefault() is not {} folder) {
					return;
				}
				
				// Process folder

				await foreach (var file in folder.GetItemsAsync().OfType<IStorageFile>()) {
					if (file.Name[(file.Name.LastIndexOf('.') + 1)..] != "txt") {
						continue;
					}
					
					Cookies.Add(Cookie.FromIStorageFile(file));
				}
			}
		);

		public ICommand Cookies_Clear => ReactiveCommand.Create(() => Cookies.Clear());

		#endregion
	}
}