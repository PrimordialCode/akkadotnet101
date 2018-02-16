namespace ConsoleApp.Shared.Messages
{
	public class InputCommandRequest : Command
	{
		public string Data { get; }

		public InputCommandRequest(string data)
		{
			Data = data;
		}
	}
}
