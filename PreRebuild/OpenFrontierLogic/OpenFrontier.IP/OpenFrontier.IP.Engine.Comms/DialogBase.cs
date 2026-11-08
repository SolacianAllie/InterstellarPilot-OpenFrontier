using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms
{
	public class DialogBase : MonoBehaviour, ICommsHandler
	{
		public bool AllowDialogToShow = true;

		protected EngineASX engine;

		public DialogStage EntryDialogStage;

		public Person Owner;

		public int UniqueId = -1;

		public Unit OwnerUnit
		{
			get
			{
				if (Owner != null)
				{
					return Owner.CurrentUnit;
				}
				return null;
			}
		}

		public Faction OwnerFaction
		{
			get
			{
				if (Owner != null)
				{
					return Owner.Faction;
				}
				return null;
			}
		}

		public Person OwnerPilot => Owner;

		public bool IsEnabled
		{
			get
			{
				return AllowDialogToShow;
			}
			set
			{
				AllowDialogToShow = value;
			}
		}

		public void Awake()
		{
			engine = EngineASX.Instance;
		}

		public DialogStage[] FindStages(bool includeInactive)
		{
			return GetComponentsInChildren<DialogStage>(includeInactive);
		}

		public ICommsStage GetStage()
		{
			return EntryDialogStage;
		}

		public void Hide()
		{
			gameObject.SetActive(value: false);
		}

		public void Show()
		{
			gameObject.SetActive(value: true);
		}
	}
}
