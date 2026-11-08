using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.BuildMode;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.BuildModePlacement
{
	public class PlacementValidator : MonoBehaviour
	{
		public TextMeshProUGUI ErrorMessageText;

		private bool isValid;

		public UnitClass UnitClass { get; set; }

		public Sector TargetSector { get; set; }

		public Transform TargetTransform { get; set; }

		public bool IsValid => isValid;

		private void Awake()
		{
			ErrorMessageText.enabled = false;
		}

		private void Update()
		{
			if (TargetTransform != null && TargetSector != null)
			{
				ErrorMessageText.text = Validate();
				isValid = string.IsNullOrEmpty(ErrorMessageText.text);
				ErrorMessageText.enabled = !IsValid;
			}
		}

		public string Validate()
		{
			if (!BuildStationValidator.CanBuild(UnitClass, TargetSector, TargetSector.ToLocalPosition(TargetTransform.position), out var errorMessage) || !BuildStationValidator.CanBuildConsideringSourcePosition(TargetSector, TargetSector.ToLocalPosition(TargetTransform.position), EngineASX.Instance.LocalUnitSector, EngineASX.Instance.LocalUnit.SectorPosition, out errorMessage))
			{
				return errorMessage;
			}
			return null;
		}
	}
}
