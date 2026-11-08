using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms
{
	public class DialogStage : MonoBehaviour, ICommsStage
	{
		public delegate void FinishedShowingHandler(DialogStage sender);

		public bool ChangeDialogEntry;

		private DialogBase dialog;

		public DialogStage NewDialogEntry;

		public DialogStage NextDialogStage;

		public int NumTimesShown;

		public int UniqueId;

		public DialogBase Dialog => dialog;

		public ICommsMessage Message => FindMessage();

		public ICommsHandler Handler => dialog;

		public event FinishedShowingHandler FinishedShowing;

		public void Awake()
		{
			if (dialog == null)
			{
				FindDialogInParents();
			}
		}

		private void FindDialogInParents()
		{
			dialog = UnityObjectHelper.FindInParentsOrSelf<DialogBase>(gameObject);
		}

		public DialogMessage FindMessage()
		{
			return GetComponentInChildren<DialogMessage>();
		}

		public void Finish(ICommsController commsController)
		{
			if (FinishedShowing != null)
			{
				FinishedShowing(this);
			}
			commsController.ChangeStage(NextDialogStage);
		}

		public IEnumerable<ICommsStageOption> GetOptions()
		{
			return GetComponentsInChildren<DialogOption>();
		}

		public void SetVisible(bool visible)
		{
			gameObject.SetActive(visible);
			if (visible && Dialog == null)
			{
				Awake();
			}
		}

		public void Show(ICommsController commsController)
		{
			NumTimesShown++;
			if (ChangeDialogEntry)
			{
				Dialog.EntryDialogStage = NewDialogEntry;
			}
		}
	}
}
