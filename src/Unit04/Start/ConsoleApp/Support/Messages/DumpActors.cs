namespace ConsoleApp.Support.Messages
{
	public class DumpActors : Shared.Messages.Command
	{
		public string SelectionPath { get; }

		public DumpActors(string selectionPath)
		{
			SelectionPath = selectionPath;
		}
	}
}
