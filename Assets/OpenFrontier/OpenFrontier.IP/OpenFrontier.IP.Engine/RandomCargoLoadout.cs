using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Unit))]
	public class RandomCargoLoadout : MonoBehaviour
	{
		public bool IgnoreCargoSpace = true;

		public int MaxQuantity = 1000;

		public int MinQuantity = 1;

		public float QuantityPower = 1f;

		private void Start()
		{
			Unit component = GetComponent<Unit>();
			EngineASX instance = EngineASX.Instance;
			if (component != null)
			{
				foreach (CargoClass cargoClass in instance.CargoClasses)
				{
					int num = MinQuantity + (int)(Mathf.Pow(Random.value, QuantityPower) * (float)(MaxQuantity - MinQuantity));
					Debug.Log($"RandomCargoLoadout: Adding {num} of \"{cargoClass.ClassName}\"", this);
					component.CargoBayComponent.AddToCargo(cargoClass, num, IgnoreCargoSpace);
				}
				return;
			}
			Debug.LogWarning("RandomCargoLoadout Not attached to a unit", this);
		}
	}
}
