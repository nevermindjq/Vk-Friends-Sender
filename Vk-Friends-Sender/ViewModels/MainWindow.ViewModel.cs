using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Platform.Storage;
using Avalonia.Threading;

using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Vk_Friends_Sender.Models;

namespace Vk_Friends_Sender.ViewModels {
	[JsonObject(MemberSerialization.OptOut)]
	public class MainWindow : ReactiveObject {
		public ObservableCollection<Proxy> Proxies { get; }
#if RELEASE
			= new();
#elif DEBUG
			= [
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
#endif

		public ObservableCollection<Account> Tokens { get; }
#if RELEASE
			= new();
#elif DEBUG
			= [
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
		];
#endif
		
		[Reactive]
		public long UserId { get; set; }

		[Reactive]
		public bool IsExecution { get; set; } = false;

		// External properties
		[JsonIgnore]
		public IStorageProvider Storage { get; set; }

		#region Proxies

		// host:port:username:password
		[JsonIgnore]
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
			},
			this.WhenAnyValue(x => x.IsExecution)
				.Select(x => !x)
		);

		[JsonIgnore]
		public ICommand Proxies_Clear => ReactiveCommand.Create(() => Proxies.Clear(), this.WhenAnyValue(x => x.IsExecution).Select(x => !x));

		#endregion

		#region Cookies

		[JsonIgnore]
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
					Tokens.Add((await reader.ReadLineAsync())!.Trim());
				}
			},
			this.WhenAnyValue(x => x.IsExecution)
				.Select(x => !x)
		);

		[JsonIgnore]
		public ICommand Cookies_Clear => ReactiveCommand.Create(() => Tokens.Clear(), this.WhenAnyValue(x => x.IsExecution).Select(x => !x));

		#endregion

		#region Control Panel

		private ICollection<Thread> _worker_threads = new List<Thread>();
		private Thread _execuition_thread;
		private ManualResetEvent _event;
		
		public ICommand Submit => ReactiveCommand.Create(
			() => {
				_execuition_thread = new(
					state => {
						var @event = (ManualResetEvent)state;
						var count = Tokens.Count;
						
						foreach (var token in Tokens.Select(x => x.Token)) {
							var thread = new Thread(
								state => {
									// TODO action

									if (Interlocked.Decrement(ref count) == 0) {
										((ManualResetEvent)state).Set();
									}
								}
							);

							thread.Start(@event);
							
							_worker_threads.Add(thread);
						}

						@event.WaitOne();
						@event.Reset();
						
						_Cancel();
					}
				);

				_event = new(false);
				_execuition_thread.Start(_event);
				
				//
				IsExecution = true;
			},
			this.WhenAnyValue(x => x.IsExecution)
				.Select(x => !x)
		);

		public ICommand Cancel => ReactiveCommand.Create(_Cancel, this.WhenAnyValue(x => x.IsExecution));

		private void _Cancel() {
			foreach (var thread in _worker_threads) {
				thread.Interrupt();
			}
			
			Dispatcher.UIThread.Invoke(() => IsExecution = false);
			
			_execuition_thread.Interrupt();
		}

		#endregion
	}
}