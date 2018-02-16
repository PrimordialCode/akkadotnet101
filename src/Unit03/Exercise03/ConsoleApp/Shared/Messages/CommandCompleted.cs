namespace ConsoleApp.Shared.Messages
{
	public class CommandCompleted
	{
		public string Data { get; }

		public CommandCompleted(string data)
		{
			Data = data;
		}
	}
}
