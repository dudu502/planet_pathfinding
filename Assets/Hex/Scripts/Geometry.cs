using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class Geometry
{

	private static int GetMidpointIndex(Dictionary<string, int> midpointIndices, List<Vector3> vertices, int i0, int i1)
	{

		var edgeKey = string.Format("{0}_{1}", Mathf.Min(i0, i1), Mathf.Max(i0, i1));

		var midpointIndex = -1;

		if (!midpointIndices.TryGetValue(edgeKey, out midpointIndex))
		{
			var v0 = vertices[i0];
			var v1 = vertices[i1];

			var midpoint = (v0 + v1) / 2f;

			if (vertices.Contains(midpoint))
				midpointIndex = vertices.IndexOf(midpoint);
			else
			{
				midpointIndex = vertices.Count;
				vertices.Add(midpoint);
			}
		}


		return midpointIndex;

	}

	/// <remarks>
	///      i0
	///     /  \
	///    m02-m01
	///   /  \ /  \
	/// i2---m12---i1
	/// </remarks>
	/// <param name="vectors"></param>
	/// <param name="indices"></param>
	public static void Subdivide(List<Vector3> vectors, List<int> indices, bool removeSourceTriangles)
	{
		var midpointIndices = new Dictionary<string, int>();

		var newIndices = new List<int>(indices.Count * 4);

		if (!removeSourceTriangles)
			newIndices.AddRange(indices);

		// Loop over each triangle and create edge midpoints
		for (int i = 0; i < indices.Count - 2; i += 3)
		{
			int i0 = indices[i];
			int i1 = indices[i + 1];
			int i2 = indices[i + 2];

			int m01 = GetMidpointIndex(midpointIndices, vectors, i0, i1);
			int m12 = GetMidpointIndex(midpointIndices, vectors, i1, i2);
			int m02 = GetMidpointIndex(midpointIndices, vectors, i2, i0);

			newIndices.AddRange(
				new[] {
				i0,m01,m02
					,
					i1,m12,m01
						,
						i2,m02,m12
						,
						m02,m01,m12
			}
			);

		}

		indices.Clear();
		indices.AddRange(newIndices);
	}

	/// <summary>
	/// create a regular icosahedron (20-sided polyhedron)
	/// </summary>
	/// <param name="primitiveType"></param>
	/// <param name="size"></param>
	/// <param name="vertices"></param>
	/// <param name="indices"></param>
	/// <remarks>
	/// You can create this programmatically instead of using the given vertex 
	/// and index list, but it's kind of a pain and rather pointless beyond a 
	/// learning exercise.
	/// </remarks>

	public static void Icosahedron(List<Vector3> vertices, List<int> indices)
	{

		indices.AddRange(
			/*new int[]
			{
			0,4,1,
			0,9,4,
			9,5,4,
			4,5,8,
			4,8,1,
			8,10,1,
			8,3,10,
			5,3,8,
			5,2,3,
			2,7,3,
			7,10,3,
			7,6,10,
			7,11,6,
			11,0,6,
			0,1,6,
			6,1,10,
			9,0,11,
			9,11,2,
			9,2,5,
			7,2,11 
		}*/
			new int[]
			{
			4,0,5,
			4,3,0,
			3,1,0,
			0,1,2,
			0,2,5,
			2,8,5,
			2,7,8,
			1,7,2,
			1,6,7,
			6,11,7,
			11,8,7,
			11,10,8,
			11,9,10,
			9,4,10,
			4,5,10,
			10,5,8,
			3,4,9,
			3,9,6,
			3,6,1,
			11,6,9
		}
		.Select(i => i + vertices.Count)
		);

		var X = 0.525731112119133606f;
		var Z = 0.850650808352039932f;

		vertices.AddRange(
			new[]
			{
			new Vector3(0f, Z, X),		//0	//4
			new Vector3(0f, Z, -X),		//1	//5
			new Vector3(Z, X, 0f),		//2	//8
			new Vector3(-Z, X, 0f),		//3	//9
			new Vector3(-X, 0f, Z),		//4	//0
			new Vector3(X, 0f, Z),		//5	//1
			new Vector3(-X, 0f, -Z),	//6	//2
			new Vector3(X, 0f, -Z),		//7	//3
			new Vector3(Z, -X, 0f),		//8	//10
			new Vector3(-Z, -X, 0f), 	//9	//11
			new Vector3(0f, -Z, X),		//10//6
			new Vector3(0f, -Z, -X)		//11//7
				
		}
		);

	}

}
