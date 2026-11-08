using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class GodModeAdvancedOptions : MonoBehaviour
	{
		public Toggle UnitUpdateEnabledToggle;

		public Toggle NpcUpdateEnabledToggle;

		public Toggle NpcPathfindingEnabledToggle;

		public Toggle NpcTargettingEnabledToggle;

		public Toggle ScanningEnabledToggle;

		public Toggle CollisionEnabledToggle;

		public Toggle FactionUpdateEnabledToggle;

		public Toggle FactionAIUpdateEnabledToggle;

		public Toggle FleetUpdateEnabledToggle;

		private void Awake()
		{
			UnitUpdateEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.UnitUpdateEnabled = value;
			});
			UnitUpdateEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.UnitUpdateEnabled;
			NpcUpdateEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.NpcUpdateEnabled;
			NpcUpdateEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.NpcUpdateEnabled = value;
			});
			NpcPathfindingEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.NpcPathfindingEnabled;
			NpcPathfindingEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.NpcPathfindingEnabled = value;
			});
			NpcTargettingEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.NpcTargettingEnabled;
			NpcTargettingEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.NpcTargettingEnabled = value;
			});
			ScanningEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.ScanningEnabled;
			ScanningEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.ScanningEnabled = value;
			});
			CollisionEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.CollisionEnabled;
			CollisionEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.CollisionEnabled = value;
			});
			FactionUpdateEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.FactionUpdateEnabled;
			FactionUpdateEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.FactionUpdateEnabled = value;
			});
			FactionAIUpdateEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.FactionAIUpdateEnabled;
			FactionAIUpdateEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.FactionAIUpdateEnabled = value;
			});
			FleetUpdateEnabledToggle.isOn = GameController.Instance.GameSettings.DebugSettings.FleetUpdateEnabled;
			FleetUpdateEnabledToggle.onValueChanged.AddListener((bool value) =>
			{
				GameController.Instance.GameSettings.DebugSettings.FleetUpdateEnabled = value;
			});
		}
	}
}
