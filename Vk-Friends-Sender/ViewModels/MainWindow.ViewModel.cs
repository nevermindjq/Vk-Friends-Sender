using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Platform.Storage;
using Avalonia.Threading;

using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Serilog;

using Vk_Friends_Sender.Exceptions;
using Vk_Friends_Sender.Models;
using Vk_Friends_Sender.Services;

namespace Vk_Friends_Sender.ViewModels {
	[JsonObject(MemberSerialization.OptOut)]
	public class MainWindow : ReactiveObject {
		private readonly AutoResetEvent _main_event = new(false);
		private readonly AutoResetEvent _groups_event = new(false);

		public bool ValidateProxies { get; set; } = false;
		
		public ObservableCollection<Proxy> Proxies { get; }
#if RELEASE
			= new();
#elif DEBUG
			= [
			"username:password@127.0.0.1:0",
			"username:password@127.0.0.1:0",
			
			"127.0.0.1:0@username:password",
			"127.0.0.1:0@username:password",
			
			"username:password:127.0.0.1:0",
			"username:password:127.0.0.1:0",
			
			"127.0.0.1:0:username:password",
			"127.0.0.1:0:username:password",
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
		
		// User Id
		[Reactive]
		public long UserId { get; set; }

		[Reactive]
		public string UserId_Error { get; set; } = "";
		
		// Api Key
		[Reactive]
		public string ApiKey { get; set; } = "";

		[Reactive]
		public string ApiKey_Error { get; set; } = "";

		private string _balance = "Balance: 0";
		public string Balance {
			get => _balance;
			set {
				if (value.StartsWith("Balance")) {
					value = value[(value.IndexOf(' ') + 1)..];
				}
				
				this.RaiseAndSetIfChanged(ref _balance, $"Balance: {value}");
			}
		}

		// Threads
		[Reactive]
		public uint Threads { get; set; }

		[Reactive]
		public string Threads_Error { get; set; } = "";

		//
		[Reactive]
		public bool IsExecution { get; set; } = false;
		
		// External properties
		[JsonIgnore]
		public IStorageProvider Storage { get; set; }

		#region Proxies
		
		[JsonIgnore]
		public ICommand Proxies_Load => ReactiveCommand.CreateFromTask(
			async () => {
				if (!_ValidateProperties()) {
					return;
				}
				
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

				IEnumerable<string> lines;
                
				using (var reader = new StreamReader(await file.OpenReadAsync())) {
					lines = (await reader.ReadToEndAsync()).Split('\n');
				}

				var count = lines.Count();
				
				int i = 0;
						
				var query = from s in lines
						let num = i++
						group s by num / Threads
						into g
						select g;

				foreach (var group in query) {
					var items = group.Count();
					
					foreach (var line in group) {
						new Thread(
							async state => {
								try {
									Proxy proxy = (string)state;

									if (!await _CheckProxyAsync(proxy)) {
										return;
									}

									Proxies.Add(proxy);
								} catch (ArgumentNullException) {
									// ignore
								} catch (ArgumentException e) {
									Log.Warning(e, "Error while parsing proxy: {proxy}", state);
								} catch (Exception e) {
									Log.Error(e, "Unknown error while parsing proxy");
								} finally {
									if (Interlocked.Decrement(ref items) == 0) {
										_groups_event.Set();
									}
									
									if (Interlocked.Decrement(ref count) == 0) {
										_main_event.Set();
									}
								}
							}
						).Start(line);
					}

					_groups_event.WaitOne();
				}

				_main_event.WaitOne();
			},
			this.WhenAnyValue(x => x.IsExecution)
				.Select(x => !x)
		);

		[JsonIgnore]
		public ICommand Proxies_Clear => ReactiveCommand.Create(() => Proxies.Clear(), this.WhenAnyValue(x => x.IsExecution).Select(x => !x));

		#endregion

		#region Tokens

		[JsonIgnore]
		public ICommand Tokens_Load => ReactiveCommand.CreateFromTask(
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
					while (!reader.EndOfStream) {
						Tokens.Add((await reader.ReadLineAsync())!.Trim());
					}
				}
			},
			this.WhenAnyValue(x => x.IsExecution)
				.Select(x => !x)
		);

		[JsonIgnore]
		public ICommand Tokens_Clear => ReactiveCommand.Create(() => Tokens.Clear(), this.WhenAnyValue(x => x.IsExecution).Select(x => !x));

		#endregion

		#region Api Key

		public ICommand AuthSolver => ReactiveCommand.CreateFromTask(() => _ValidateApiTokenAsync(ApiKey));

		#endregion
		
		#region Control Panel

		private readonly ICollection<Thread> _worker_threads = new List<Thread>();
		private TwoCaptcha.TwoCaptcha? _solver;
		private Thread? _execution_thread;
		private int _proxy_index = 0;
		
		public ICommand Submit => ReactiveCommand.CreateFromTask(
			async () => {
				if (!_ValidateProperties() || !await _ValidateApiTokenAsync(ApiKey)) {
					return;
				}
				
				_execution_thread = new(
					() => {
						var count = Tokens.Count;

						int i = 0;
						
						var query = from s in Tokens
								let num = i++
								group s by num / Threads
								into g
								select g;
						
						foreach (var group in query) {
							var items = group.Count();
							
							foreach (var account in group) {
								var thread = new Thread(
									async () => {
										var proxy = Proxies[_proxy_index];
										
										while (!await _CheckProxyAsync(proxy)) {
											Proxies.RemoveAt(_proxy_index);
											_proxy_index--;

											proxy = Proxies[_proxy_index];
										}
										
										using (var vk = new Vk(proxy, account.Token, solver: _solver)) {
											try {
												await vk.AddToFriendsAsync(UserId);
											} catch (TokenExpiredException) {
												Tokens.Remove(account);
											}
											catch (Exception e) {
												Log.Error(e, "Error while execution adding");
											}
										}

										if (Interlocked.Increment(ref _proxy_index) == Proxies.Count) {
											_proxy_index = 0;
										}

										if (Interlocked.Decrement(ref items) == 0) {
											_groups_event.Set();
										}

										if (Interlocked.Decrement(ref count) == 0) {
											_main_event.Set();
										}
									}
								);

								thread.Start();
							
								_worker_threads.Add(thread);
							}

							try {
								_groups_event.WaitOne();
							}
							catch {
								// ignore
							}
						}

						try {
							_main_event.WaitOne();
						}
						catch {
							// ignore
						}
						
						_Cancel();
					}
				);

				_execution_thread.Start();
				
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
			
			_execution_thread?.Interrupt();
		}

		#endregion

		private async Task<bool> _CheckProxyAsync(Proxy proxy) {
			if (!ValidateProxies) {
				return true;
			}
            
			var handler = new HttpClientHandler {
				UseCookies = false,
				UseProxy = true,
				Proxy = (WebProxy)proxy
			};

			using (var http = new HttpClient(handler, true)) {
				HttpResponseMessage response;
				
				try {
					response = await http.GetAsync("https://api.ipify.org");
				} catch {
					return false;
				}

				if (!response.IsSuccessStatusCode) {
					return false;
				}
			}

			return true;
		}
		
		private async Task<bool> _ValidateApiTokenAsync(string api_key) {
			_solver = new(api_key);

			try {
				Balance = $"{await _solver.Balance()}";
			} catch {
				Balance = "0";
				_solver = null;
				return false;
			}

			return true;
		}

		private bool _ValidateProperties() {
			var is_bad = false;
			
			if (UserId == 0) {
				UserId_Error = "User Id is invalid";
				is_bad = true;
			}

			if (string.IsNullOrEmpty(ApiKey)) {
				ApiKey_Error = "Api Key required";
				is_bad = true;
			}

			if (Threads == 0) {
				Threads_Error = "Threads is invalid";
				is_bad = true;
			}

			return !is_bad;
		}
	}
}