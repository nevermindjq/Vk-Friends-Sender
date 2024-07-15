namespace Vk_Friends_Sender.Models {
	public class Account {
		public required string Token { get; set; }
		
		public static implicit operator Account(string str) => new() {
			Token = str,
		};
	}
}