using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class MainCamera : MonoBehaviour
	{
		public StarParticleSystem StarParticleSystem;

		private ShakeModule cameraShake;

		public ShakeModule ShakeModule => cameraShake;

		public Vector3 GetSectorPosition()
		{
			return transform.position - EngineASX.Instance.ActiveSector.transform.position;
		}

		public void SetWorldPosition(Vector3 worldPosition)
		{
			if (cameraShake != null && Time.timeScale > 0f)
			{
				worldPosition += cameraShake.CurrentOffset;
			}
			if (EngineASX.Instance.ActiveSector != null)
			{
				transform.position = worldPosition;
			}
		}

		public void SetSectorPosition(Vector3 sectorPosition)
		{
			if (cameraShake != null && Time.timeScale > 0f)
			{
				sectorPosition += cameraShake.CurrentOffset;
			}
			if (EngineASX.Instance.ActiveSector != null)
			{
				transform.localPosition = EngineASX.Instance.ActiveSector.transform.position + sectorPosition;
			}
		}

		private void Awake()
		{
		}

		private void Start()
		{
			cameraShake = GetComponent<ShakeModule>();
		}

		private void Update()
		{
		}
	}
}
