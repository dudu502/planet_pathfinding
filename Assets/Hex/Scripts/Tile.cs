using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public enum TileDisplayOptions
{
	None,
	GroupID,
	NavWeight,
	Navigable
}

[Serializable]
public class Tile : MonoBehaviour, IHeapItem<Tile>
{
	public int HeapIndex { set; get; }
	public int CompareTo(Tile other)
	{
		int compare = Nav.f.CompareTo(other.Nav.f);
		if (compare == 0)
			compare = Nav.h.CompareTo(other.Nav.h);
		return -compare;
	}
	public class NavData
	{
		public float h,g,f;
		public Tile LastTile;
		public Vector3 Position;

        public void Clear()
		{
			h = g = f = 0;
			LastTile = null;
		}
	}
	public NavData Nav = new NavData();
	public static float planetScale;

	public static Action<Tile> OnTileClickedAction;
	public static Action<Tile> OnTileMouseEnter;
	public static Action<Tile> OnTileMouseExit;

	[Tooltip("The instance of the hexsphere which constructed this tile")]
	public Hexsphere parentPlanet;

	public List<Tile> neighborTiles;

	//Tile Attributes
	[Tooltip("Whether or not navigation will consider this tile as a valid to move over")]
	public bool navigable = true;
	[Tooltip("The cost of moving across this tile in terms of pathfinding weight.  Pathfinding will prioritize the lowest cost path.")]
	[Range(1, 100)]
	public int pathCost = 1;

	// The center of this tile when initially generated.  Does not account for extrusion.
	public Vector3 center
	{
		get { return tileRenderer.bounds.center; }
	}

	// The current center of the tiles face accounting for extrusion.
	public Vector3 FaceCenter
	{
		get
		{
			float heightMult = IsInverted ? -1f : 1f;
			return transform.position + transform.up * ExtrudedHeight * heightMult * parentPlanet.planetScale;
		}
	}

	//The position of this tile as reported by the renderer in world space.  More strict than the above center.
	public Vector3 centerRenderer
	{
		get { return tileRenderer.bounds.center; }
	}

	[HideInInspector]
	public Renderer tileRenderer;

	[HideInInspector]
	public float ExtrudedHeight;

	public bool isHexagon;

	[HideInInspector]
	public bool IsInverted;

	public List<GameObject> PlacedObjects = new List<GameObject>();

	[HideInInspector]
	public TileDisplayOptions InfoDisplayOption;

	//Used to specify which tile is currently selected so that any tile can query the selected tile or assign themselves as selected.
	private static Tile selectedTile;
	//The center of the tile in worldspace as assigned by the hexsphere during generation.  Not affected by the scale of the planet.
	[SerializeField, HideInInspector]
	private bool hasBeenExtruded;

	[SerializeField, HideInInspector]
	private Material TileMaterial;

	private Color HilightColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

	[SerializeField, HideInInspector]
	private Vector3[] Vertices;
	[SerializeField, HideInInspector]
	private Vector2[] UVs;
	[SerializeField, HideInInspector]
	private int[] Triangles;

	void Awake()
	{
		tileRenderer = GetComponent<Renderer>(); 
		Nav.Position = transform.position / parentPlanet.planetScale;
	}
	
	public void Initialize()
	{
		tileRenderer = GetComponent<Renderer>();
	}

	public Vector2 GetCoordinates()
	{
		Vector2 latLong = Vector2.zero;
		if (parentPlanet)
		{
			Vector3 ToTile = transform.position - parentPlanet.transform.position;
			float Latitude = 90.0f - Vector3.Angle(ToTile, parentPlanet.transform.up);
			Vector3 ToTileHoriz = Vector3.ProjectOnPlane(ToTile, parentPlanet.transform.up);
			float Longitude = Vector3.SignedAngle(ToTileHoriz, parentPlanet.transform.forward, parentPlanet.transform.up);
			latLong.x = Latitude;
			latLong.y = Longitude;
		}

		return latLong;
	}

	void OnMouseEnter()
	{
		OnTileMouseEnter?.Invoke(this);
	}
	void OnMouseExit()
	{
		OnTileMouseExit?.Invoke(this);
	}

	void OnMouseDown()
	{
		//Demo function
		//pathfindingDrawDemo ();
		if (OnTileClickedAction != null)
		{
			OnTileClickedAction.Invoke(this);
		}
	}




	public void placeObject(GameObject obj)
	{
		obj.transform.position = FaceCenter;
		obj.transform.up = transform.up;
		obj.transform.SetParent(transform);
		PlacedObjects.Add(obj);
	}

	public void DeleteLastPlacedObject()
	{
		if (PlacedObjects.Count > 0)
		{
			DestroyImmediate(PlacedObjects[PlacedObjects.Count - 1]);
			PlacedObjects.RemoveAt(PlacedObjects.Count - 1);
		}
	}

	public void DeletePlacedObjects()
	{
		for (int i = 0; i < PlacedObjects.Count; i++)
		{
			if (PlacedObjects[i] != null)
			{
				DestroyImmediate(PlacedObjects[i]);
			}
		}

		PlacedObjects.Clear();
	}

	public void SetExtrusionHeight(float height)
	{
		float delta = height - ExtrudedHeight;
		Extrude(delta);
	}

	public void Extrude(float heightDelta)
	{
		ExtrudedHeight += heightDelta;
		Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
		Vector3[] verts = mesh.vertices;
		//Check if this tile has already been extruded
		if (hasBeenExtruded)
		{
			int sides = isHexagon ? 6 : 5;
			//Apply extrusion heights
			for (int i = 0; i < sides; i++)
			{
				Vector3 worldV = (transform.TransformPoint(verts[i]) - parentPlanet.transform.position);
				worldV += heightDelta * worldV.normalized * parentPlanet.planetScale;
				verts[i] = transform.InverseTransformPoint(worldV + parentPlanet.transform.position);
			}
			for (int i = sides + 2; i < sides + sides * 4; i += 4)
			{
				Vector3 worldV = (transform.TransformPoint(verts[i]) - parentPlanet.transform.position);
				worldV += heightDelta * worldV.normalized * parentPlanet.planetScale;
				verts[i] = transform.InverseTransformPoint(worldV + parentPlanet.transform.position);

				worldV = (transform.TransformPoint(verts[i + 1]) - parentPlanet.transform.position);
				worldV += heightDelta * worldV.normalized * parentPlanet.planetScale;
				verts[i + 1] = transform.InverseTransformPoint(worldV + parentPlanet.transform.position);
			}

			mesh.vertices = verts;

			// If this has a mesh collider, update the mesh
			MeshCollider mCollider = GetComponent<MeshCollider>();
			if (mCollider != null)
			{
				GetComponent<MeshCollider>().sharedMesh = mesh;
			}

			GetComponent<MeshFilter>().sharedMesh = mesh;
			return;
		}

		//Sort vertices clockwise
		Array.Sort(verts, new ClockwiseComparer(transform.InverseTransformPoint(center)));
		List<int> tris = new List<int>(mesh.triangles);
		//List<Vector3> normals = new List<Vector3> (mesh.normals);

		//Duplicate the existing vertices
		List<Vector3> faceVerts = new List<Vector3>(verts);
		//Translate duplicated verts along local up
		for (int i = 0; i < faceVerts.Count; i++)
		{
			Vector3 worldV = (transform.TransformPoint(faceVerts[i]) - parentPlanet.transform.position);
			worldV += heightDelta * worldV.normalized * parentPlanet.planetScale;
			faceVerts[i] = transform.InverseTransformPoint(worldV + parentPlanet.transform.position);
		}
		//Set triangles for extruded face
		tris[0] = 0;
		tris[1] = 1;
		tris[2] = 2;

		tris[3] = 0;
		tris[4] = 2;
		tris[5] = 3;

		tris[6] = 0;
		tris[7] = 3;
		tris[8] = 4;

		//Only set the last triangle if this is a hexagon
		if (verts.Length == 6)
		{
			tris[9] = 0;
			tris[10] = 4;
			tris[11] = 5;
		}
		int t = 0;
		//Create side triangles
		for (int i = 0; i < verts.Length - 1; i++, t += 4)
		{
			faceVerts.Add(verts[i]);
			faceVerts.Add(verts[i + 1]);

			faceVerts.Add(faceVerts[i]);
			faceVerts.Add(faceVerts[i + 1]);

			tris.Add(t + verts.Length);
			tris.Add(t + verts.Length + 1);
			tris.Add(t + verts.Length + 2);

			tris.Add(t + verts.Length + 1);
			tris.Add(t + verts.Length + 3);
			tris.Add(t + verts.Length + 2);
		}
		//Manually create last two triangles
		faceVerts.Add(verts[verts.Length - 1]);
		faceVerts.Add(verts[0]);

		faceVerts.Add(faceVerts[verts.Length - 1]);
		faceVerts.Add(faceVerts[0]);

		tris.Add(faceVerts.Count - 4);
		tris.Add(faceVerts.Count - 3);
		tris.Add(faceVerts.Count - 2);

		tris.Add(faceVerts.Count - 3);
		tris.Add(faceVerts.Count - 1);
		tris.Add(faceVerts.Count - 2);


		mesh.vertices = faceVerts.ToArray();
		mesh.triangles = tris.ToArray();
		mesh.RecalculateNormals();
		//Reassign UVs
		mesh.uv = isHexagon ? generateHexUvs() : generatePentUvs();

		//Assign meshes to Mesh Collider and Mesh Filter
		if (GetComponent<MeshCollider>() != null)
		{
			GetComponent<MeshCollider>().sharedMesh = mesh;
		}

		GetComponent<MeshFilter>().sharedMesh = mesh;
		hasBeenExtruded = true;

	}

	public Vector2[] generateHexUvs()
	{
		Vector2[] uvs = new Vector2[30];
		uvs[0] = new Vector2(0.293f, 0.798f);
		uvs[1] = new Vector2(0.397f, 0.977f);
		uvs[2] = new Vector2(0.604f, 0.977f);
		uvs[3] = new Vector2(0.707f, 0.798f);
		uvs[4] = new Vector2(0.604f, 0.619f);
		uvs[5] = new Vector2(0.397f, 0.619f);

		float h = 6f;
		float y = 0.6f;
		for (int i = 6; i < 28; i += 4)
		{
			uvs[i] = new Vector2(h / 6f, 0f);
			uvs[i + 1] = new Vector2((h - 1) / 6f, 0f);

			uvs[i + 2] = new Vector2(h / 6f, y);
			uvs[i + 3] = new Vector2((h - 1) / 6f, y);
			h--;
		}
		return uvs;
	}

	public Vector2[] generatePentUvs()
	{
		Vector2[] uvs = new Vector2[25];
		uvs[0] = new Vector2(0.389f, 0.97f);
		uvs[1] = new Vector2(0.611f, 0.97f);
		uvs[2] = new Vector2(0.68f, 0.758f);
		uvs[3] = new Vector2(0.5f, 0.627f);
		uvs[4] = new Vector2(0.32f, 0.758f);

		float h = 5f;
		float y = 0.6f;
		for (int i = 5; i < 22; i += 4)
		{
			uvs[i] = new Vector2(h / 5f, 0f);
			uvs[i + 1] = new Vector2((h - 1) / 5f, 0f);

			uvs[i + 2] = new Vector2(h / 5f, y);
			uvs[i + 3] = new Vector2((h - 1) / 5f, y);
			h--;
		}
		return uvs;
	}

	

	public void SetColor(Color col)
	{
		Material tempMaterial = new Material(GetComponent<Renderer>().sharedMaterial);
		tempMaterial.color = col;
		tileRenderer.sharedMaterial = tempMaterial;
	}

	public void SetMaterial(Material mat)
	{
		TileMaterial = mat;
		tileRenderer.sharedMaterial = mat;
	}

	public void SetHighlight(bool hilighted)
	{
		if (hilighted)
		{

			Material tempMaterial = new Material(TileMaterial);
			tempMaterial.color = HilightColor + TileMaterial.color;
			tileRenderer.sharedMaterial = tempMaterial;
		}
		else
		{
			tileRenderer.sharedMaterial = TileMaterial;
		}
	}

	


	public void SaveMeshData()
	{
		MeshFilter mf = GetComponent<MeshFilter>();
		Vertices = mf.sharedMesh.vertices;
		Triangles = mf.sharedMesh.triangles;
		UVs = mf.sharedMesh.uv;
	}

	public void RestoreMesh()
	{
		MeshFilter mf = GetComponent<MeshFilter>();
		if (mf.sharedMesh == null)
		{
			Mesh mesh = new Mesh();
			mesh.vertices = Vertices;
			mesh.triangles = Triangles;
			mesh.uv = UVs;

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();

			mf.sharedMesh = mesh;
		}
	}
}

public class ClockwiseComparer : IComparer
{
	private Vector3 mOrigin;

	public ClockwiseComparer(Vector3 origin)
	{
		mOrigin = origin;
	}

	public int Compare(object first, object second)
	{
		Vector3 v1 = (Vector3)first;
		Vector3 v2 = (Vector3)second;

		return IsClockwise(v2, v1, mOrigin);
	}

	public static int IsClockwise(Vector3 first, Vector3 second, Vector3 origin)
	{
		if (first == second)
		{
			return 0;
		}

		Vector3 firstOffset = first - origin;
		Vector3 secondOffset = second - origin;

		float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.z);
		float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.z);

		if (angle1 < angle2)
		{
			return 1;
		}

		if (angle1 > angle2)
		{
			return -1;
		}

		return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? 1 : -1;
	}
}

public class ClockwiseComparer2D : IComparer
{
	private Vector2 mOrigin;

	public ClockwiseComparer2D(Vector2 origin)
	{
		mOrigin = origin;
	}

	public int Compare(object first, object second)
	{
		Vector2 v1 = (Vector2)first;
		Vector2 v2 = (Vector2)second;

		return IsClockwise(v2, v1, mOrigin);
	}

	public static int IsClockwise(Vector2 first, Vector2 second, Vector2 origin)
	{
		if (first == second)
		{
			return 0;
		}

		Vector2 firstOffset = first - origin;
		Vector2 secondOffset = second - origin;

		float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.y);
		float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.y);

		if (angle1 < angle2)
		{
			return 1;
		}

		if (angle1 > angle2)
		{
			return -1;
		}

		return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? 1 : -1;
	}
}
