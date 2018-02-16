namespace ConsoleApp.Shared.Messages
{
	public class InputCommand : Command
	{
		public string Data { get; }

		public InputCommand(string data)
		{
			Data = data;
		}
	}
}
