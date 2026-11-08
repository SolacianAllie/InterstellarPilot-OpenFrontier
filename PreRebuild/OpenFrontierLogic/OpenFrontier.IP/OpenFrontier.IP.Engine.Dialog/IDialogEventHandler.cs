namespace OpenFrontier.IP.Engine.Dialog
{
	public interface IDialogEventHandler
	{
		string GetMessage(DialogRequestArguments? dialogRequestArguments);
	}
}
