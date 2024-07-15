using Avalonia.Platform.Storage;

namespace Vk_Friends_Sender.Models {
	public class Cookie {
		public string Filename { get; init; } = "Some cookie.txt";
		public string Filepath { get; init; } = "/home/username/cookies/cookie.txt";

		public static Cookie FromIStorageFile(IStorageFile file) =>
				new() {
					Filename = file.Name,
					Filepath = file.Path.AbsolutePath
				};
	}
}