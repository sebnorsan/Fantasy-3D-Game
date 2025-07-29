#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TerrainTreeConverterWindow : EditorWindow
{
	private Terrain terrain;
	private Transform treeParent;
	private bool removeTreesFromTerrain = true;
	private bool markAsStatic = true;

	[MenuItem("Tools/Terrain Tree to Prefab Converter...")]
	public static void ShowWindow()
	{
		var window = GetWindow<TerrainTreeConverterWindow>("Tree Converter");
		window.minSize = new Vector2(400, 200);
	}

	void OnGUI()
	{
		GUILayout.Label("Convert Terrain Trees to Prefab GameObjects", EditorStyles.boldLabel);

		terrain = EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true) as Terrain;
		treeParent = EditorGUILayout.ObjectField("Parent Transform", treeParent, typeof(Transform), true) as Transform;

		removeTreesFromTerrain = EditorGUILayout.Toggle("Remove Original Trees", removeTreesFromTerrain);
		markAsStatic = EditorGUILayout.Toggle("Mark New Trees Static", markAsStatic);

		EditorGUILayout.Space();

		if (GUILayout.Button("Convert Trees"))
		{
			if (terrain == null)
			{
				EditorUtility.DisplayDialog("Error", "Please assign a Terrain.", "OK");
			}
			else if (treeParent == null)
			{
				EditorUtility.DisplayDialog("Error", "Please assign a Parent Transform.", "OK");
			}
			else
			{
				ConvertTreesToPrefabs();
			}
		}
	}

	void ConvertTreesToPrefabs()
	{
		TerrainData terrainData = terrain.terrainData;
		Vector3 terrainPosition = terrain.transform.position;
		TreeInstance[] treeInstances = terrainData.treeInstances;
		TreePrototype[] treePrototypes = terrainData.treePrototypes;

		int count = treeInstances.Length;
		if (count == 0)
		{
			Debug.Log("No trees to convert on the terrain.");
			return;
		}

		Undo.RegisterCompleteObjectUndo(treeParent.gameObject, "Convert Terrain Trees");
		if (removeTreesFromTerrain)
		{
			Undo.RegisterCompleteObjectUndo(terrainData, "Clear Terrain Trees");
		}

		for (int i = 0; i < count; i++)
		{
			var tree = treeInstances[i];
			var proto = treePrototypes[tree.prototypeIndex];
			var prefab = proto.prefab;

			if (prefab == null)
			{
				Debug.LogWarning($"Tree prototype at index {tree.prototypeIndex} has no prefab.");
				continue;
			}

			Vector3 worldPos = Vector3.Scale(tree.position, terrainData.size) + terrainPosition;
			Quaternion rotation = Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f);
			Vector3 scale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

			GameObject newTree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, treeParent);
			newTree.transform.position = worldPos;
			newTree.transform.rotation = rotation;
			newTree.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);

			if (markAsStatic)
				newTree.isStatic = true;

			Undo.RegisterCreatedObjectUndo(newTree, "Create Tree Prefab");
		}

		if (removeTreesFromTerrain)
		{
			terrainData.treeInstances = new TreeInstance[0];
			Debug.Log("Removed original terrain trees.");
		}

		Debug.Log($"Converted {count} terrain trees to prefabs under '{treeParent.name}'.");
	}
}
#endif