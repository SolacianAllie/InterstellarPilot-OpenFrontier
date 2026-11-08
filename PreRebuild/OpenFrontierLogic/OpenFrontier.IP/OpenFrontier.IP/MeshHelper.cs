using UnityEngine;

namespace OpenFrontier.IP
{
	public static class MeshHelper
	{
		public static void CreatePlaneMesh(ref Matrix4x4 vertexTransform, Vector3[] vertices, Vector2[] uv, int[] triangles, ref int vertexStartIndex, ref int uvStartIndex, ref int triangleStartIndex)
		{
			vertices[vertexStartIndex] = vertexTransform.MultiplyPoint(new Vector3(0.5f, 0f, 0.5f));
			vertices[vertexStartIndex + 1] = vertexTransform.MultiplyPoint(new Vector3(0.5f, 0f, -0.5f));
			vertices[vertexStartIndex + 2] = vertexTransform.MultiplyPoint(new Vector3(-0.5f, 0f, 0.5f));
			vertices[vertexStartIndex + 3] = vertexTransform.MultiplyPoint(new Vector3(-0.5f, 0f, -0.5f));
			uv[uvStartIndex] = new Vector2(1f, 1f);
			uv[uvStartIndex + 1] = new Vector2(1f, 0f);
			uv[uvStartIndex + 2] = new Vector2(0f, 1f);
			uv[uvStartIndex + 3] = new Vector2(0f, 0f);
			triangles[triangleStartIndex] = vertexStartIndex;
			triangles[triangleStartIndex + 1] = vertexStartIndex + 1;
			triangles[triangleStartIndex + 2] = vertexStartIndex + 2;
			triangles[triangleStartIndex + 3] = vertexStartIndex + 2;
			triangles[triangleStartIndex + 4] = vertexStartIndex + 1;
			triangles[triangleStartIndex + 5] = vertexStartIndex + 3;
			vertexStartIndex += 4;
			uvStartIndex += 4;
			triangleStartIndex += 6;
		}

		public static void CreateTriangle(ref Matrix4x4 vertexTransform, Vector3[] vertices, Vector2[] uv, int[] triangles, ref int vertexStartIndex, ref int uvStartIndex, ref int triangleStartIndex)
		{
			vertices[vertexStartIndex] = vertexTransform.MultiplyPoint(new Vector3(0.5f, 0f, 0f));
			vertices[vertexStartIndex + 1] = vertexTransform.MultiplyPoint(new Vector3(-0.5f, 0f, 0f));
			vertices[vertexStartIndex + 2] = vertexTransform.MultiplyPoint(new Vector3(0f, 0f, 1f));
			uv[uvStartIndex] = new Vector2(0f, 0f);
			uv[uvStartIndex + 1] = new Vector2(1f, 0f);
			uv[uvStartIndex + 2] = new Vector2(0.5f, 1f);
			triangles[triangleStartIndex] = vertexStartIndex;
			triangles[triangleStartIndex + 1] = vertexStartIndex + 1;
			triangles[triangleStartIndex + 2] = vertexStartIndex + 2;
			vertexStartIndex += 3;
			uvStartIndex += 3;
			triangleStartIndex += 3;
		}

		public static Mesh CreatePlaneMesh()
		{
			Mesh mesh = new Mesh();
			Vector3[] vertices = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			int[] triangles = new int[6];
			Matrix4x4 vertexTransform = Matrix4x4.identity;
			int vertexStartIndex = 0;
			int uvStartIndex = 0;
			int triangleStartIndex = 0;
			CreatePlaneMesh(ref vertexTransform, vertices, uv, triangles, ref vertexStartIndex, ref uvStartIndex, ref triangleStartIndex);
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.triangles = triangles;
			return mesh;
		}
	}
}
