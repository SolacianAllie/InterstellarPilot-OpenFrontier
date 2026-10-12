using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.BuildMode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.BuildModePlacement
{
	public class BuildModePlacementScreen : EngineScreen
	{
		public delegate void OnBuiltHandler(bool preferExit);

		public delegate bool RequestBuildHandler(UnitClass unitClass);

		private int oldCredits = -1;

		public TextMeshProUGUI CreditsLabel;

		public PlacementValidator PlacementValidator;

		public PlacementUnitTransformer PlacementUnitTransformer;

		public Button BuildAndExitButton;

		public Button BuildAndContinueButton;

		private ActiveUnit activeUnitPreview;

		private Vector3 cameraInitialDirectionFromNewStation = Vector3.zero;

		public float CamAngleY;

		public float CamAngleX = 45f;

		private float distanceFromNewStation;

		public float DefaultDistanceFromNewStation = 20f;

		public float CameraDistanceRadiusMultiplier = 1.2f;

		public float InitialPlacementOffsetFromLocalUnit = 100f;

		private float time;

		public float NewStationFadeRateMultiplier = 2f;

		public float NewStationMinAlpha = 0.3f;

		public float NewStationMaxAlpha = 0.8f;

		public MeshRenderer PlacementRadiusRenderer;

		public Color PlacementRadiusRendererValidColor = Color.green;

		public Color PlacementRadiusRendererInvalidColor = Color.red;

		public float PlacementRadiusRendererRadiusMultiplier = 1f;

		public Transform TargetTransform;

		public UnitClass UnitClass { get; set; }

		public event OnBuiltHandler OnBuilt;

		public event RequestBuildHandler RequestBuild;

		protected override void awake()
		{
			base.awake();
			BuildAndExitButton.onClick.AddListener(BuildAndExitButtonClick);
			BuildAndContinueButton.onClick.AddListener(BuildButtonClick);
			PlacementUnitTransformer.Target = TargetTransform;
		}

		protected override void refresh()
		{
			base.refresh();
			Cleanup();
			activeUnitPreview = EngineASX.Instance.InstantiateActiveUnit(UnitClass);
			activeUnitPreview.transform.SetParent(TargetTransform);
			activeUnitPreview.transform.localScale = Vector3.one;
			activeUnitPreview.transform.localPosition = Vector3.zero;
			SetInitialPlacementPosition();
			activeUnitPreview.ActiveUnitRenderMode = ActiveUnitRenderMode.Fade;
			activeUnitPreview.SetFadeAlpha(0.5f, null);
			distanceFromNewStation = DefaultDistanceFromNewStation + UnitClass.ShieldRingRadius * CameraDistanceRadiusMultiplier;
			Vector3 position = EngineASX.Instance.LocalUnit.transform.position;
			Vector3 position2 = activeUnitPreview.transform.position;
			position.y = 0f;
			position2.y = 0f;
			CamAngleY = Unit.GetYBearing(position2, position);
			cameraInitialDirectionFromNewStation = Vector3.forward;
			TargetTransform.localRotation = Quaternion.LookRotation(EngineASX.Instance.LocalUnit.transform.forward, Vector3.up);
			PlacementValidator.UnitClass = UnitClass;
			PlacementValidator.TargetSector = EngineASX.Instance.LocalPlayerSector;
			PlacementValidator.TargetTransform = TargetTransform;
			EngineASX.Instance.TrySetCameraSpectatorEnabled(enabled: false);
			RefreshCreditsLabel();
		}

		private void RefreshCreditsLabel()
		{
			CreditsLabel.text = TextFormattingHelper.FormatCredits(EngineASX.Instance.LocalFaction.Credits, includeSuffix: true);
		}

		private void SetInitialPlacementPosition()
		{
			TargetTransform.position = EngineASX.Instance.LocalUnit.transform.position + EngineASX.Instance.LocalUnit.transform.forward * (EngineASX.Instance.LocalUnit.UnitClass.ShieldRingRadius + UnitClass.ShieldRingRadius + InitialPlacementOffsetFromLocalUnit);
		}

		private void BuildAndExitButtonClick()
		{
			BuildNewStation(preferExit: true);
		}

		private void BuildButtonClick()
		{
			BuildNewStation(preferExit: false);
		}

		protected override bool onNavigatingBack()
		{
			Cleanup();
			return base.onNavigatingBack();
		}

		private void Cleanup()
		{
			if (activeUnitPreview != null)
			{
				Object.Destroy(activeUnitPreview.gameObject);
			}
		}

		protected override void update()
		{
			base.update();
			if (activeUnitPreview != null)
			{
				BuildAndExitButton.interactable = PlacementValidator.IsValid;
				BuildAndContinueButton.interactable = BuildModeHelper.CanBuildMultiple(UnitClass) && BuildModeHelper.CanAffordToBuildMultiple(UnitClass) && PlacementValidator.IsValid;
				UpdatePlacementWidget();
			}
			if (oldCredits != EngineASX.Instance.LocalFaction.Credits)
			{
				oldCredits = EngineASX.Instance.LocalFaction.Credits;
				RefreshCreditsLabel();
			}
		}

		private void UpdatePlacementWidget()
		{
			PlacementRadiusRenderer.material.color = (PlacementValidator.IsValid ? PlacementRadiusRendererValidColor : PlacementRadiusRendererInvalidColor);
			float num = UnitClass.ShieldRingRadius * PlacementRadiusRendererRadiusMultiplier;
			PlacementRadiusRenderer.transform.localScale = new Vector3(num, 0f, num);
			PlacementRadiusRenderer.transform.localPosition = Vector3.down * Mathf.Max(3f, UnitClass.ShieldRingRadius * 0.5f);
		}

		protected override void lateUpdate()
		{
			if (activeUnitPreview != null)
			{
				TransformCamera();
				UpdatePreviewUnitFade();
			}
		}

		private void UpdatePreviewUnitFade()
		{
			time += RealTime.deltaTime * NewStationFadeRateMultiplier;
			float alpha = Mathf.Lerp(NewStationMinAlpha, NewStationMaxAlpha, (Mathf.Sin(time) + 1f) / 2f);
			activeUnitPreview.SetFadeAlpha(alpha, null);
		}

		private void TransformCamera()
		{
			GameController.Instance.MainCamera.transform.position = GetCameraPosition();
			GameController.Instance.MainCamera.transform.LookAt(activeUnitPreview.transform);
		}

		private Vector3 GetCameraPosition()
		{
			Quaternion quaternion = Quaternion.Euler(0f - CamAngleX, CamAngleY, 0f);
			return activeUnitPreview.transform.position + quaternion * cameraInitialDirectionFromNewStation * distanceFromNewStation;
		}

		public bool TryBuildNewStation(bool preferExit)
		{
			if (RequestBuild != null && !RequestBuild(UnitClass))
			{
				return false;
			}
			BuildNewStation(preferExit);
			return true;
		}

		public void BuildNewStation(bool preferExit)
		{
			Vector3 sectorPosition = EngineASX.Instance.ActiveSector.ToLocalPosition(activeUnitPreview.transform.position);
			sectorPosition.y = 0f;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(UnitClass.UnitPrefab, EngineASX.Instance.LocalPlayerSector, sectorPosition, 10f, addCargoLoadout: false, UnitClass.UnitType != UnitType.Ship);
			unit.transform.localRotation = Quaternion.LookRotation(activeUnitPreview.transform.forward, Vector3.up);
			unit.Faction = EngineASX.Instance.LocalFaction;
			EngineASX.Instance.LocalFaction.ApplyTransaction(-UnitClass.SaleCost, FactionTransactionType.StationBuild, null, null, null, UnitClass);
			EngineASX.Instance.CreditsAnimation.AllowCreditBeepAudio = true;
			EngineASX.Instance.NotifyNewStationConstructionStarted(unit);
			if (preferExit)
			{
				Cleanup();
			}
			if (OnBuilt != null)
			{
				OnBuilt(preferExit);
			}
		}
	}
}
