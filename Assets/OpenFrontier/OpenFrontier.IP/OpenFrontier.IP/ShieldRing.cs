using System;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class ShieldRing : MonoBehaviour
	{
		public const float SideLengthMultiplier = 0.95f;

		public const int vertexCount = 24;

		private static Color depletedShieldColor = new Color(0f, 0f, 0f, 0f);

		public float BarAlpha = 0.4f;

		private float lastTotalShieldPoints = -1f;

		public Material LineMaterial;

		public float LineThickness = 0.75f;

		public float RadiusMultiplier = 1f;

		public bool RecreateMesh = true;

		private Mesh ringMesh;

		public Vector3 TranslationFromUnit = new Vector3(0f, 0f, -1.2f);

		public Unit Unit;

		private Color[] vertexColors = new Color[24];

		public static float PolygonInRadiusSideLength(float radius, int sides)
		{
			return 2f * radius * Mathf.Tan(MathF.PI / (float)sides);
		}

		public static float PolygonCircumRadiusSideLength(float radius, int sides)
		{
			return 2f * radius * Mathf.Sign(MathF.PI / (float)sides);
		}

		public void FindUnit()
		{
			Unit = UnityObjectHelper.FindInParentsOrSelf<Unit>(gameObject);
		}

		private static Mesh CreateRingMesh(float radius, float lineThickness)
		{
			Mesh mesh = new Mesh();
			Vector3[] vertices = new Vector3[24];
			Vector2[] uv = new Vector2[24];
			int[] triangles = new int[36];
			int vertexStartIndex = 0;
			int uvStartIndex = 0;
			int triangleStartIndex = 0;
			float num = PolygonInRadiusSideLength(radius, 6);
			for (int i = 0; i < 6; i++)
			{
				float y = (float)i * 60f;
				Vector3 vector = new Vector3(0f, 0f, radius);
				Quaternion quaternion = Quaternion.Euler(new Vector3(0f, y, 0f));
				vector = quaternion * vector;
				Matrix4x4 vertexTransform = Matrix4x4.TRS(vector, quaternion, new Vector3(num * 0.95f, 1f, lineThickness));
				MeshHelper.CreatePlaneMesh(ref vertexTransform, vertices, uv, triangles, ref vertexStartIndex, ref uvStartIndex, ref triangleStartIndex);
			}
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.triangles = triangles;
			return mesh;
		}

		private void CreateRingMesh()
		{
			ringMesh = CreateRingMesh(RadiusMultiplier, LineThickness);
			RecreateMesh = false;
		}

		private void LateUpdate()
		{
			if (!(Unit != null) || Unit.IsDestroyed || !Unit.ShieldEnabled)
			{
				return;
			}
			if (RecreateMesh)
			{
				CreateRingMesh();
			}
			if (ringMesh != null)
			{
				float currentTotalShieldPoints = Unit.Components.ShieldComponent.CurrentTotalShieldPoints;
				if (currentTotalShieldPoints != lastTotalShieldPoints)
				{
					lastTotalShieldPoints = currentTotalShieldPoints;
					setColors();
				}
				drawShieldRing();
			}
		}

		private void drawShieldRing()
		{
			Matrix4x4 matrix = Matrix4x4.TRS(Unit.transform.TransformPoint(TranslationFromUnit), Quaternion.Euler(0f, Unit.transform.eulerAngles.y, 0f), Vector3.one);
			Graphics.DrawMesh(ringMesh, matrix, LineMaterial, 0);
		}

		private void setColors()
		{
			for (int i = 0; i < 6; i++)
			{
				Color color = default;
				if (!Unit.Components.ShieldComponent.IsShieldDepleted(i))
				{
					color = Unit.Engine.GetUnitShieldColor(Unit, i);
					color.a = BarAlpha;
				}
				else
				{
					color = depletedShieldColor;
				}
				for (int j = 0; j < 4; j++)
				{
					int num = i * 4 + j;
					vertexColors[num] = color;
				}
			}
			ringMesh.colors = vertexColors;
		}
	}
}
