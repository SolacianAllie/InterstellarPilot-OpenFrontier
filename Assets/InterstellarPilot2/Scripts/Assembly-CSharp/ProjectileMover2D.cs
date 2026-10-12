using UnityEngine;

public class ProjectileMover2D : MonoBehaviour
{
	public float speed = 15f;

	public float hitOffset;

	public bool UseFirePointRotation;

	public Vector3 rotationOffset = new Vector3(0f, 0f, 0f);

	public GameObject hit;

	public GameObject flash;

	private Rigidbody2D rb;

	public GameObject[] Detached;

	private void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		if (flash != null)
		{
			GameObject gameObject = Object.Instantiate(flash, transform.position, Quaternion.identity);
			gameObject.transform.forward = base.gameObject.transform.forward;
			ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
			if (component != null)
			{
				Object.Destroy(gameObject, component.main.duration);
			}
			else
			{
				ParticleSystem component2 = gameObject.transform.GetChild(0).GetComponent<ParticleSystem>();
				Object.Destroy(gameObject, component2.main.duration);
			}
		}
		Object.Destroy(base.gameObject, 5f);
	}

	private void FixedUpdate()
	{
		if (speed != 0f)
		{
			rb.linearVelocity = transform.forward * speed;
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		rb.constraints = RigidbodyConstraints2D.FreezeAll;
		speed = 0f;
		ContactPoint2D contactPoint2D = collision.contacts[0];
		Quaternion rotation = Quaternion.FromToRotation(Vector3.up, contactPoint2D.normal);
		Vector3 position = contactPoint2D.point + contactPoint2D.normal * hitOffset;
		if (hit != null)
		{
			GameObject gameObject = Object.Instantiate(hit, position, rotation);
			if (UseFirePointRotation)
			{
				gameObject.transform.rotation = base.gameObject.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
			}
			else if (rotationOffset != Vector3.zero)
			{
				gameObject.transform.rotation = Quaternion.Euler(rotationOffset);
			}
			else
			{
				gameObject.transform.LookAt(contactPoint2D.point + contactPoint2D.normal);
			}
			ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
			if (component != null)
			{
				Object.Destroy(gameObject, component.main.duration);
			}
			else
			{
				ParticleSystem component2 = gameObject.transform.GetChild(0).GetComponent<ParticleSystem>();
				Object.Destroy(gameObject, component2.main.duration);
			}
		}
		GameObject[] detached = Detached;
		foreach (GameObject gameObject2 in detached)
		{
			if (gameObject2 != null)
			{
				gameObject2.transform.parent = null;
			}
		}
		Object.Destroy(base.gameObject);
	}
}
