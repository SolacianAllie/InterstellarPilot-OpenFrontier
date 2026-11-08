using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Comms
{
	public interface ICommsStage
	{
		ICommsMessage Message { get; }

		ICommsHandler Handler { get; }

		IEnumerable<ICommsStageOption> GetOptions();

		void SetVisible(bool visible);

		void Show(ICommsController commsController);

		void Finish(ICommsController commsController);
	}
}
