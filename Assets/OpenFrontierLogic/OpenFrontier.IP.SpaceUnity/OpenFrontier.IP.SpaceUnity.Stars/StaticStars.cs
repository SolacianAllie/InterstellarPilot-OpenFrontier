using UnityEngine;

namespace OpenFrontier.IP.SpaceUnity.Stars
{
	/// <summary>
	/// OpenFrontier-native static starfield. Renders a huge six-face cuboid around the play
	/// area with the stars texture applied to every face, so the stars never parallax.
	/// Own replacement for the vendored "Space for Unity" component that was lost from the project.
	/// Like the original (AGENTS.md): the dome is PINNED AT THE WORLD ORIGIN every frame and
	/// the DeepSpace SpaceCamera sits at the sector anchor with far plane 100000 looking in.
	/// </summary>
	[DisallowMultipleComponent]
	public class StaticStars : MonoBehaviour
	{
		[Tooltip("Star map texture; tiled once per face of the dome cube.")]
		public Texture2D starsTexture;

		public Color starsColor = new Color(1f, 1f, 1f, 1f);

		public float starsIntensity = 1f;

		[Tooltip("Force the dome back to the world origin every frame (matches the original dome behaviour).")]
		public bool pinToWorldOrigin = true;

		public MeshFilter MeshFilter;

		public MeshRenderer MeshRenderer;

		[SerializeField] private Mesh _mesh;

		[SerializeField] private float _side = 20000f;

		private const string StarsShaderName = "OpenFrontier/Stars";

		private void OnEnable()
		{
			if (MeshFilter == null && (MeshFilter = GetComponent<MeshFilter>()) == null)
			{
				MeshFilter = gameObject.AddComponent<MeshFilter>();
			}
			if (MeshRenderer == null && (MeshRenderer = GetComponent<MeshRenderer>()) == null)
			{
				MeshRenderer = gameObject.AddComponent<MeshRenderer>();
			}
			if (_mesh == null || MeshFilter.sharedMesh == null)
			{
				CreateCustomMesh();
			}
			MeshFilter.sharedMesh = _mesh;
			UpdateMaterial();
		}

		private void CreateCustomMesh()
		{
			_mesh = GenCube(_side);
			if (MeshFilter != null)
			{
				MeshFilter.sharedMesh = _mesh;
			}
		}

		private void LateUpdate()
		{
			if (pinToWorldOrigin)
			{
				transform.position = Vector3.zero;
			}
		}

		/// <summary>Applies starsTexture/starsColor/starsIntensity to the dome material (shader "OpenFrontier/Stars").</summary>
		public void UpdateMaterial()
		{
			if (MeshRenderer == null)
			{
				return;
			}
			Material material = MeshRenderer.sharedMaterial;
			if (material == null || material.shader == null || material.shader.name != StarsShaderName)
			{
				Shader shader = Shader.Find(StarsShaderName);
				if (shader == null)
				{
					Debug.LogWarning("StaticStars: shader \"" + StarsShaderName + "\" not found; star dome cannot render.", this);
					return;
				}
				material = new Material(shader);
				material.name = "SU_StaticStars (runtime)";
				MeshRenderer.sharedMaterial = material;
			}
			// Property names by string: no Unity native calls in field initializers / static ctors (AGENTS.md).
			material.SetTexture("_MainTex", starsTexture);
			material.SetColor("_StarsColor", starsColor);
			material.SetFloat("_StarsIntensity", starsIntensity);
			MeshRenderer.enabled = true;
		}

		/// <summary>Builds a 6-face cuboid with outward quads (double-sided from inside via shader Cull Front).</summary>
		private Mesh GenCube(float _sideLength)
		{
			float halfSide = _sideLength * 0.5f;
			// (u, v) chosen so cross(u, v) equals the outward face normal d for each face.
			Vector3[] dirs =
			{
				new Vector3( 1f,  0f,  0f), new Vector3( 0f,  1f,  0f), new Vector3( 0f,  0f,  1f),
				new Vector3(-1f,  0f,  0f), new Vector3( 0f, -1f,  0f), new Vector3( 0f,  0f, -1f)
			};
			Vector3[] us =
			{
				new Vector3( 0f,  0f, -1f), new Vector3( 1f,  0f,  0f), new Vector3( 1f,  0f,  0f),
				new Vector3( 0f,  0f,  1f), new Vector3( 1f,  0f,  0f), new Vector3(-1f,  0f,  0f)
			};
			Vector3[] vs =
			{
				new Vector3( 0f,  1f,  0f), new Vector3( 0f,  0f, -1f), new Vector3( 0f,  1f,  0f),
				new Vector3( 0f,  1f,  0f), new Vector3( 0f,  0f,  1f), new Vector3( 0f,  1f,  0f)
			};

			// corners relative to face center p = d*h:
			// A = (d-u-v)h [bl], B = (d+u-v)h [br], C = (d+u+v)h [tr], D = (d-u+v)h [tl]
			Vector3[] vertices = new Vector3[24];
			Vector3[] normals = new Vector3[24];
			Vector2[] uvs = new Vector2[24];
			int[] triangles = new int[36];

			int index = 0;
			for (int face = 0; face < 6; face++)
			{
				Vector3 d = dirs[face];
				Vector3 u = us[face];
				Vector3 v = vs[face];
				Vector3 p = d * halfSide;                       // face center
				Vector3[] faceVerts =
				{
					(d - u - v) * halfSide,                     // A: bottom-left (seen from outside)
					(d + u - v) * halfSide,                     // B: bottom-right
					(d + u + v) * halfSide,                     // C: top-right
					(d - u + v) * halfSide                      // D: top-left
				};
				for (int i = 0; i < 4; i++)
				{
					vertices[index + i] = faceVerts[i];
					normals[index + i] = d;
					uvs[index + i] = new Vector2((i == 1 || i == 2) ? 1f : 0f, (i == 2 || i == 3) ? 1f : 0f);
				}
				// Clockwise when viewed from outside, so the shader (Cull Front) shows the inner surface.
				triangles[index + 0] = index;
				triangles[index + 1] = index + 2;
				triangles[index + 2] = index + 1;
				triangles[index + 3] = index;
				triangles[index + 4] = index + 3;
				triangles[index + 5] = index + 1;
				index += 4;
			}

			Mesh mesh = new Mesh();
			mesh.name = "OpenFrontierStaticStarsCube";
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uvs;
			mesh.triangles = triangles;
			return mesh;
		}
	}
}
