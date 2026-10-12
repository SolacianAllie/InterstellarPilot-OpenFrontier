using OpenFrontier.IP.Engine.CargoFactory;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoFactory
{
	public class CargoFactoryProfileItemUI : MonoBehaviour
	{
		public CargoFactoryProfileItemComponentUI ComponentPrefab;

		public CargoFactoryProfileItemCreditsUI ConsumerRevenuePrefab;

		public Transform SumRoot;

		public Text TextPrefab;

		public void Refresh(CargoFactoryProfileItem profile)
		{
			UnityObjectHelper.DestroyChildren(SumRoot.gameObject, destroyImmediate: true);
			AddInputs(profile);
			AddEqualsSymbol();
			if (profile.IsConsumer)
			{
				AddConsumerRevenue(profile);
			}
			else
			{
				AddOutputs(profile);
			}
		}

		private void AddConsumerRevenue(CargoFactoryProfileItem item)
		{
			CargoFactoryProfileItemCreditsUI cargoFactoryProfileItemCreditsUI = Object.Instantiate(ConsumerRevenuePrefab);
			cargoFactoryProfileItemCreditsUI.CargoFactoryProfileItem = item;
			cargoFactoryProfileItemCreditsUI.transform.SetParent(SumRoot, worldPositionStays: false);
			cargoFactoryProfileItemCreditsUI.Refresh();
		}

		private void AddOutputs(CargoFactoryProfileItem profileItem)
		{
			int num = 0;
			foreach (CargoFactoryProfileItemOutput output in profileItem.Outputs)
			{
				if (num > 0)
				{
					AddPlusSymbol();
				}
				AddFactoryComponent(output);
				num++;
			}
		}

		private void AddInputs(CargoFactoryProfileItem profileItem)
		{
			int num = 0;
			foreach (CargoFactoryProfileItemInput input in profileItem.Inputs)
			{
				if (num > 0)
				{
					AddPlusSymbol();
				}
				AddFactoryComponent(input);
				num++;
			}
		}

		private void AddEqualsSymbol()
		{
			Text text = Object.Instantiate(TextPrefab);
			text.text = "=";
			text.transform.SetParent(SumRoot, worldPositionStays: false);
		}

		private void AddFactoryComponent(CargoFactoryProfileItemComponent input)
		{
			CargoFactoryProfileItemComponentUI cargoFactoryProfileItemComponentUI = Object.Instantiate(ComponentPrefab);
			cargoFactoryProfileItemComponentUI.CargoFactoryProfileItemComponent = input;
			cargoFactoryProfileItemComponentUI.transform.SetParent(SumRoot, worldPositionStays: false);
			cargoFactoryProfileItemComponentUI.Refresh();
		}

		private void AddPlusSymbol()
		{
			Text text = Object.Instantiate(TextPrefab);
			text.text = "+";
			text.transform.SetParent(SumRoot, worldPositionStays: false);
		}
	}
}
