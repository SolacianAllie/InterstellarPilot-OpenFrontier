using OpenFrontier.IP.Engine;
using UnityEngine;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI.Screens.BuildModePlacement
{
	public class PlacementUnitTransformer : MonoBehaviour
	{
		public float RotateRate = 1f;

		public float TranslationRate = 10f;

		public Transform Target;

		public RepeatableButton RotateLeftButton;

		public RepeatableButton RotateRightButton;

		public RepeatableButton TranslateUpButton;

		public RepeatableButton TranslateDownButton;

		public RepeatableButton TranslateLeftButton;

		public RepeatableButton TranslateRightButton;

		private void Update()
		{
			if (Target != null)
			{
				if (RotateLeftButton.IsPressed || Input.GetKey(KeyCode.Q))
				{
					Rotate(-1f);
				}
				if (RotateRightButton.IsPressed || Input.GetKey(KeyCode.E))
				{
					Rotate(1f);
				}
				if (TranslateUpButton.IsPressed || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
				{
					Translate(Vector3.forward);
				}
				if (TranslateDownButton.IsPressed || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
				{
					Translate(Vector3.back);
				}
				if (TranslateLeftButton.IsPressed || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
				{
					Translate(Vector3.left);
				}
				if (TranslateRightButton.IsPressed || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
				{
					Translate(Vector3.right);
				}
			}
		}

		private void Translate(Vector3 vector3)
		{
			Quaternion quaternion = Quaternion.Euler(new Vector3(0f, GameController.Instance.MainCamera.transform.rotation.eulerAngles.y, 0f));
			Target.Translate(quaternion * vector3 * TranslationRate * RealTime.deltaTime, Space.World);
		}

		private void Rotate(float turn)
		{
			Target.Rotate(Vector3.up, turn * RotateRate * RealTime.deltaTime, Space.Self);
		}
	}
}
