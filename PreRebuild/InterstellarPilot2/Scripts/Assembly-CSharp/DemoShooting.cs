using UnityEngine;
using Input = OpenFrontier.LegacyInput;

public class DemoShooting : MonoBehaviour
{
	public GameObject FirePoint;

	public Camera Cam;

	public float MaxLength;

	public GameObject[] Prefabs;

	private Ray RayMouse;

	private Vector3 direction;

	private Quaternion rotation;

	[Header("GUI")]
	private float windowDpi;

	private int Prefab;

	private GameObject Instance;

	private float hSliderValue = 0.1f;

	private float fireCountdown;

	private float buttonSaver;

	public Animation camAnim;

	private void Start()
	{
		if (Screen.dpi < 1f)
		{
			windowDpi = 1f;
		}
		if (Screen.dpi < 200f)
		{
			windowDpi = 1f;
		}
		else
		{
			windowDpi = Screen.dpi / 200f;
		}
		Counter(0);
	}

	private void Update()
	{
		if (Input.GetButtonDown("Fire1"))
		{
			camAnim.Play(camAnim.clip.name);
			Object.Instantiate(Prefabs[Prefab], FirePoint.transform.position, FirePoint.transform.rotation);
		}
		if (Input.GetMouseButton(1) && fireCountdown <= 0f)
		{
			Object.Instantiate(Prefabs[Prefab], FirePoint.transform.position, FirePoint.transform.rotation);
			fireCountdown = 0f;
			fireCountdown += hSliderValue;
		}
		fireCountdown -= Time.deltaTime;
		if ((Input.GetKey(KeyCode.A) || Input.GetAxis("Horizontal") < 0f) && buttonSaver >= 0.4f)
		{
			buttonSaver = 0f;
			Counter(-1);
		}
		if ((Input.GetKey(KeyCode.D) || Input.GetAxis("Horizontal") > 0f) && buttonSaver >= 0.4f)
		{
			buttonSaver = 0f;
			Counter(1);
		}
		buttonSaver += Time.deltaTime;
		if (Cam != null)
		{
			Vector3 mousePosition = Input.mousePosition;
			RayMouse = Cam.ScreenPointToRay(mousePosition);
			if (Physics.Raycast(RayMouse.origin, RayMouse.direction, out var hitInfo, MaxLength))
			{
				RotateToMouseDirection(gameObject, hitInfo.point);
			}
		}
		else
		{
			Debug.Log("No camera");
		}
	}

	private void OnGUI()
	{
		GUI.Label(new Rect(10f * windowDpi, 5f * windowDpi, 400f * windowDpi, 20f * windowDpi), "Use left mouse button to single shoot!");
		GUI.Label(new Rect(10f * windowDpi, 25f * windowDpi, 400f * windowDpi, 20f * windowDpi), "Use and hold the right mouse button for quick shooting!");
		GUI.Label(new Rect(10f * windowDpi, 45f * windowDpi, 400f * windowDpi, 20f * windowDpi), "Fire rate:");
		hSliderValue = GUI.HorizontalSlider(new Rect(70f * windowDpi, 50f * windowDpi, 100f * windowDpi, 20f * windowDpi), hSliderValue, 0f, 1f);
		GUI.Label(new Rect(10f * windowDpi, 65f * windowDpi, 400f * windowDpi, 20f * windowDpi), "Use the keyboard buttons A/<- and D/-> to change projectiles!");
	}

	private void Counter(int count)
	{
		Prefab += count;
		if (Prefab > Prefabs.Length - 1)
		{
			Prefab = 0;
		}
		else if (Prefab < 0)
		{
			Prefab = Prefabs.Length - 1;
		}
	}

	private void RotateToMouseDirection(GameObject obj, Vector3 destination)
	{
		direction = destination - obj.transform.position;
		rotation = Quaternion.LookRotation(direction);
		obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, rotation, 1f);
	}
}
