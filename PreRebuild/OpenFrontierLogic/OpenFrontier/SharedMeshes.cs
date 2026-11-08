using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: shared runtime-built meshes (avoids builtin resource
	/// lookups and duplicate generation).
	/// </summary>
	public static class SharedMeshes
	{
		private static Mesh quad;

		// Unit quad in the XY plane, front face looking down -Z (same
		// orientation as Unity's builtin Quad).
		public static Mesh Quad
		{
			get
			{
				if (quad == null)
				{
					quad = BuildQuad();
				}
				return quad;
			}
		}

		private static Mesh BuildQuad()
		{
			Mesh mesh = new Mesh();
			mesh.name = "SharedQuad";
			mesh.vertices = new Vector3[]
			{
				new Vector3(-0.5f, -0.5f, 0f),
				new Vector3(0.5f, -0.5f, 0f),
				new Vector3(0.5f, 0.5f, 0f),
				new Vector3(-0.5f, 0.5f, 0f)
			};
			mesh.normals = new Vector3[]
			{
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward
			};
			mesh.uv = new Vector2[]
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(0f, 1f)
			};
			mesh.triangles = new int[] { 0, 3, 2, 0, 2, 1 };
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}
