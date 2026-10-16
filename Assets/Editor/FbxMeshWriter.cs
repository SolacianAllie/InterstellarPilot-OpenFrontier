using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenFrontier.Editor
{
	/// <summary>
	/// Writes an ASCII FBX 7.4 file from raw mesh arrays, with no dependency on
	/// Unity and none on the com.unity.formats.fbx package.
	/// </summary>
	/// <remarks>
	/// WHY THIS EXISTS
	///
	/// Unity's own exporter only walks GameObjects, its menu is registered
	/// under GameObject so it never appears for a Project-window asset, and the
	/// API returns null with no message for anything else. Rather than keep
	/// wrapping assets to feed it, this writes the file directly.
	///
	/// NO UnityEngine BY DESIGN. This type takes float[] and int[] so it can be
	/// compiled and run outside Unity against synthetic meshes, which is the
	/// only way to check an FBX writer without a Blender in the loop. The
	/// Unity-facing half is FbxMeshExporter.
	///
	/// THE TWO THINGS THAT SILENTLY PRODUCE A BROKEN FILE
	///
	/// 1. PolygonVertexIndex marks the LAST vertex of every face by its
	///    bitwise complement, written as -index-1. A file that writes plain
	///    positive indices everywhere is not rejected -- it imports as a
	///    mangled, self-intersecting mesh. For triangles that is
	///    i0, i1, ~i2; for quads i0, i1, i2, ~i3.
	///
	/// 2. Array headers are *N where N is the count of DOUBLES, not of
	///    elements. "Vertices: *24" is 24 doubles, i.e. 8 vertices. Write the
	///    element count instead and every consumer reads past the end of the
	///    buffer.
	///
	/// LAYERELEMENTS are written ByControlPoint + Direct, one entry per vertex,
	/// which is valid because this mesh carries per-vertex normals, tangents
	/// and UVs. ByPolygonVertex + IndexToDirect is the alternative and would
	/// need three entries per triangle instead.
	/// </remarks>
	public static class FbxMeshWriter
	{
		/// <summary>Formats a float the way FBX ASCII wants: invariant, round-trippable.</summary>
		private static string N(float value)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				// A NaN reaching the file corrupts the parse of everything
				// after it, so it is caught here rather than shipped.
				throw new ArgumentException("mesh contains a non-finite value");
			}

			return value.ToString("R", CultureInfo.InvariantCulture);
		}

		private static string N(int value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		private static void Require(bool condition, string message)
		{
			if (!condition)
			{
				throw new ArgumentException(message);
			}
		}

		/// <summary>
		/// Emits an FBX ASCII array property: "*count { a: v,v,v,... }".
		/// </summary>
		private static void WriteArray(StringBuilder sb, string name,
			IList<string> values)
		{
			// The count is the number of scalars, not of elements -- see the
			// remarks. Written as the literal number of entries in `values`.
			sb.Append("\t\t").Append(name).Append(": *").Append(values.Count)
				.Append(" {\n");
			sb.Append("\t\t\ta: ");

			for (int i = 0; i < values.Count; i++)
			{
				if (i > 0)
				{
					sb.Append(',');
				}

				sb.Append(values[i]);
			}

			sb.Append("\n\t\t}\n");
		}

		/// <summary>
		/// Builds the complete FBX file text.
		/// </summary>
		/// <param name="objectName">Name for the Model/Geometry nodes.</param>
		/// <param name="positions">xyz triples, length = vertexCount * 3.</param>
		/// <param name="normals">xyz triples, or null. Length must match positions.</param>
		/// <param name="tangents">xyzw quads, or null. Length must match positions.</param>
		/// <param name="uvs">uv pairs, or null. Length must match positions.</param>
		/// <param name="triangles">Triangle indices, length a multiple of 3.</param>
		/// <param name="flipWinding">
		/// Reverse every triangle. Unity is left-handed and Blender's FBX
		/// importer is right-handed, so a file can come out inside out and
		/// there is no way to know from here which convention the consumer
		/// applies. Rather than guess, this is a switch: if the imported mesh
		/// looks inside out, export again with this on.
		/// </param>
		public static string Build(string objectName, float[] positions,
			float[] normals, float[] tangents, float[] uvs, int[] triangles,
			bool flipWinding = false)
		{
			Require(objectName != null && objectName.Length > 0,
				"objectName is required");
			Require(positions != null && positions.Length > 0, "positions are required");
			Require(positions.Length % 3 == 0,
				"positions length " + positions.Length + " is not a multiple of 3");
			Require(triangles != null && triangles.Length > 0, "triangles are required");
			Require(triangles.Length % 3 == 0,
				"triangles length " + triangles.Length + " is not a multiple of 3");

			int vertexCount = positions.Length / 3;
			int faceCount = triangles.Length / 3;

			CheckLayer("normals", normals, vertexCount, 3);
			CheckLayer("tangents", tangents, vertexCount, 4);
			CheckLayer("uvs", uvs, vertexCount, 2);

			// A face index outside the vertex buffer produces a file that
			// imports as garbage, so it is refused rather than written.
			for (int i = 0; i < triangles.Length; i++)
			{
				Require(triangles[i] >= 0 && triangles[i] < vertexCount,
					"triangle index " + triangles[i] + " at " + i +
					" is outside the " + vertexCount + " vertices");
			}

			const int geometryId = 140000000;
			const int modelId = 200000000;
			const int layerElementIndex = 0;

			StringBuilder sb = new StringBuilder(1 << 16);

			sb.Append("; FBX 7.4.0 project file\n");
			sb.Append("; Generated by OpenFrontier FbxMeshWriter -- geometry only.\n");
			sb.Append("; Unity is Y-up left-handed; the axis block below declares the\n");
			sb.Append("; file Y-up, so importers convert rather than guess.\n");
			sb.Append("FBXHeaderExtension:  {\n");
			sb.Append("\tFBXHeaderVersion: 1003\n");
			sb.Append("\tFBXVersion: 7400\n");
			sb.Append("\tCreator: \"OpenFrontier FbxMeshWriter\"\n");
			sb.Append("}\n");

			// Declares the convention the data is already in, so the importer
			// converts from it instead of assuming the FBX default.
			sb.Append("GlobalSettings:  {\n");
			sb.Append("\tVersion: 1000\n");
			sb.Append("\tProperties70:  {\n");
			Axis(sb, "UpAxis", 1);
			Axis(sb, "UpAxisSign", 1);
			Axis(sb, "FrontAxis", 2);
			Axis(sb, "FrontAxisSign", 1);
			Axis(sb, "CoordAxis", 0);
			Axis(sb, "CoordAxisSign", 1);
			Axis(sb, "OriginalUpAxis", -1);
			Axis(sb, "OriginalUpAxisSign", 1);
			sb.Append("\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",1\n");
			sb.Append("\t}\n");
			sb.Append("}\n");

			sb.Append("Definitions:  {\n");
			sb.Append("\tVersion: 100\n");
			sb.Append("\tCount: 3\n");
			sb.Append("\tObjectType: \"GlobalSettings\" {Count: 1}\n");
			sb.Append("\tObjectType: \"Geometry\" {Count: 1}\n");
			sb.Append("\tObjectType: \"Model\" {Count: 1}\n");
			sb.Append("}\n");

			sb.Append("Objects:  {\n");
			sb.Append("\tGeometry: ").Append(geometryId).Append(", \"Geometry::\", \"Mesh\" {\n");

			var vertList = new List<string>(positions.Length);
			for (int i = 0; i < positions.Length; i++)
			{
				vertList.Add(N(positions[i]));
			}

			WriteArray(sb, "Vertices", vertList);

			// The complement trick, applied per triangle: ~i2 == -i2-1.
			var pvi = new List<string>(triangles.Length);
			for (int f = 0; f < faceCount; f++)
			{
				int i0 = triangles[(f * 3) + 0];
				int i1 = triangles[(f * 3) + 1];
				int i2 = triangles[(f * 3) + 2];

				if (flipWinding)
				{
					int swap = i1;
					i1 = i2;
					i2 = swap;
				}

				pvi.Add(N(i0));
				pvi.Add(N(i1));
				pvi.Add(N(-i2 - 1));
			}

			WriteArray(sb, "PolygonVertexIndex", pvi);

			sb.Append("\t\tGeometryVersion: 124\n");

			if (normals != null)
			{
				WriteLayerElement(sb, "LayerElementNormal", "Normals", normals,
					vertexCount);
			}

			if (tangents != null)
			{
				WriteLayerElement(sb, "LayerElementTangent", "Tangents", tangents,
					vertexCount);
			}

			if (uvs != null)
			{
				WriteLayerElement(sb, "LayerElementUV", "UV", uvs, vertexCount);
			}

			if (normals != null || tangents != null || uvs != null)
			{
				sb.Append("\t\tLayer: ").Append(layerElementIndex).Append(" {\n");
				sb.Append("\t\t\tVersion: 100\n");

				if (normals != null)
				{
					WriteLayerRef(sb, "LayerElementNormal", layerElementIndex);
				}

				if (tangents != null)
				{
					WriteLayerRef(sb, "LayerElementTangent", layerElementIndex);
				}

				if (uvs != null)
				{
					WriteLayerRef(sb, "LayerElementUV", layerElementIndex);
				}

				sb.Append("\t\t}\n");
			}

			sb.Append("\t}\n");

			sb.Append("\tModel: ").Append(modelId).Append(", \"Model::\", \"Mesh\" {\n");
			sb.Append("\t\tVersion: 232\n");
			sb.Append("\t\tProperties70:  {\n");
			sb.Append("\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",0,0,0\n");
			sb.Append("\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",0,0,0\n");
			sb.Append("\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",1,1,1\n");
			sb.Append("\t\t}\n");
			sb.Append("\t\tShading: T\n");
			sb.Append("\t\tCulling: \"CullingOff\"\n");
			sb.Append("\t}\n");
			sb.Append("}\n");

			sb.Append("Connections:  {\n");
			// Geometry -> Model, then Model -> the scene root (0).
			sb.Append("\tC: \"OO\",").Append(geometryId).Append(',').Append(modelId)
				.Append('\n');
			sb.Append("\tC: \"OO\",").Append(modelId).Append(",0\n");
			sb.Append("}\n");

			return sb.ToString();
		}

		private static void Axis(StringBuilder sb, string name, int value)
		{
			sb.Append("\t\tP: \"").Append(name)
				.Append("\", \"int\", \"Integer\", \"\",").Append(N(value)).Append('\n');
		}

		private static void WriteLayerRef(StringBuilder sb, string type, int index)
		{
			sb.Append("\t\t\tLayerElement:  {Type: \"").Append(type)
				.Append("\", TypedIndex: ").Append(N(index)).Append("}\n");
		}

		private static void WriteLayerElement(StringBuilder sb, string element,
			string property, float[] data, int vertexCount)
		{
			sb.Append("\t\t").Append(element).Append(": 0 {\n");
			sb.Append("\t\t\tVersion: 101\n");
			sb.Append("\t\t\tName: \"\"\n");
			sb.Append("\t\t\tMappingInformationType: \"ByControlPoint\"\n");
			sb.Append("\t\t\tReferenceInformationType: \"Direct\"\n");

			var values = new List<string>(data.Length);
			for (int i = 0; i < data.Length; i++)
			{
				values.Add(N(data[i]));
			}

			WriteArrayIndented(sb, property, values, 3);
			sb.Append("\t\t}\n");
		}

		/// <summary>WriteArray with a deeper indent, for layer element bodies.</summary>
		private static void WriteArrayIndented(StringBuilder sb, string name,
			IList<string> values, int depth)
		{
			string indent = new string('\t', depth);
			sb.Append(indent).Append(name).Append(": *").Append(values.Count)
				.Append(" {\n");
			sb.Append(indent).Append("\ta: ");

			for (int i = 0; i < values.Count; i++)
			{
				if (i > 0)
				{
					sb.Append(',');
				}

				sb.Append(values[i]);
			}

			sb.Append('\n').Append(indent).Append("}\n");
		}

		private static void CheckLayer(string name, float[] layer, int vertexCount,
			int componentsPerVertex)
		{
			if (layer == null)
			{
				return;
			}

			// Each layer has its OWN width: position and normal are 3, tangent
			// is 4 (xyzw, the w being handedness) and UV is 2. Checking them
			// all against the position length rejects any mesh that has
			// tangents or UVs at all -- which is every real planet mesh.
			int expected = vertexCount * componentsPerVertex;
			Require(layer.Length == expected,
				name + " has " + layer.Length + " floats but " + vertexCount +
				" vertices x " + componentsPerVertex + " components = " +
				expected);
		}
	}
}