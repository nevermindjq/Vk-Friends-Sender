using System;
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

using Serilog;

using Vk_Friends_Sender.Exceptions;
using Vk_Friends_Sender.Models;
using Vk_Friends_Sender.Services;

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
		private readonly ManualResetEvent _main_event = new(false);
		private readonly ManualResetEvent _groups_event = new(false);
		private TwoCaptcha.TwoCaptcha? _solver;
		private Thread? _execution_thread;
		private int _proxy_index = 0;
		
		public ICommand Submit => ReactiveCommand.CreateFromTask(
			async () => {
				if (!_ValidateProperties() || !await _ValidateApiTokenAsync(ApiKey)) {
					return;
				}
				
				_execution_thread = new(
					state => {
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
										using (var vk = new Vk(Proxies[_proxy_index], account.Token, solver: _solver)) {
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
								_groups_event.Reset();
							}
							catch {
								// ignore
							}
						}

						try {
							_main_event.WaitOne();
							_main_event.Reset();
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