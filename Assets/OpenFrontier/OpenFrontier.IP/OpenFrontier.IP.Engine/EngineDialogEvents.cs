using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class EngineDialogEvents : MonoBehaviour
	{
		public DialogEvent PlayerEnteredOwnedPilottedShip;

		public DialogEvent AttackTargetIsDockedEvent;

		public DialogEvent AttackedByUnfriendly;

		public DialogEvent AttackedByNeutral;

		public DialogEvent AttackedByFriendly;

		public DialogEvent CargoBayFull;

		public DialogEvent CountermeasureDeploy;

		public DialogEvent Destroyed;

		public DialogEvent DestroyedTarget;

		public DialogEvent GotTarget;

		public DialogEvent HullDamage;

		public DialogEvent InterceptingShip;

		public DialogEvent InterceptingStation;

		public DialogEvent LostTarget;

		public DialogEvent LostTargetCloaked;

		public DialogEvent MiningFinished;

		public DialogEvent MissileLock;

		public DialogEvent PlayerStartDestroyMission;

		public DialogEvent ShieldsDown;

		public DialogEvent TakeDamage;

		public DialogEvent CargoStolenByOther;

		public DialogEvent CrashedIntoNpcShip;

		public DialogEvent NpcCrashedIntoSuperior;

		public DialogEvent OutlawRandomAttack;

		public DialogEvent FleetMovingToNewSector;

		public DialogEvent FleetReceivedNewOrder;

		public DialogEvent FleetRepairOrderComplete;

		public DialogEvent MercenarySalesPitch;

		public DialogEvent MercenaryHired;

		public DialogEvent[] LoadDialogEvents()
		{
			return Resources.LoadAll<DialogEvent>(GameController.Instance.DialogEventsPath);
		}
	}
}
