namespace ConsoleApp.Shared.Messages
{
	/// <summary>
	/// A sample base class that is used to implement Commands in a raw CQRS pattern.
	/// </summary>
	public abstract class Command
	{
		/// <summary>
		/// Context information attached to the message
		/// </summary>
		public CommandContext Context { get; private set; } = new CommandContext();
	}

	public class CommandContext
	{
		/// <summary>
		/// The UserId will contribute to identify the
		/// instance of the command handler to which route the message.
		/// </summary>
		public string UserId { get; set; }
	}
}
