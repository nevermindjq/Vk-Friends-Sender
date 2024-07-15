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
#endif
		];

		public ObservableCollection<Account> Cookies { get; } = [
#if DEBUG
			"Some token",
			"Some token",
			"Some token",
			"Some token",
			"Some token",
			"Some token",
			"Some token",
			"Some token",
			"Some token",
			"Some token",
			"Some token",
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

				using (var reader = new StreamReader(await file.OpenReadAsync())) {
					while (!reader.EndOfStream) {
						Proxies.Add((await reader.ReadLineAsync())!.Trim());
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
				var options = new FilePickerOpenOptions {
					AllowMultiple = false,
					FileTypeFilter = [
						FilePickerFileTypes.TextPlain, 
					],
					Title = "Tokens picker"
				};

				if (await Storage.OpenFilePickerAsync(options) is not {Count: > 0} files || files.FirstOrDefault() is not {} file) {
					return;
				}
				
				// Process file
				
				using (var reader = new StreamReader(await file.OpenReadAsync())) {
					Cookies.Add((await reader.ReadLineAsync())!.Trim());
				}
			}
		);

		public ICommand Cookies_Clear => ReactiveCommand.Create(() => Cookies.Clear());

		#endregion
	}
}