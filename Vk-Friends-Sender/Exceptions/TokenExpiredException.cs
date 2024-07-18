using System;

namespace Vk_Friends_Sender.Exceptions {
	public class TokenExpiredException(string token) : ArgumentException("[VK API] Token has expired", token);
}