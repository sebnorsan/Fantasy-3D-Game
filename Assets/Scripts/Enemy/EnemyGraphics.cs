using System.Collections.Generic;
using UnityEngine;

public class EnemyGraphics : MonoBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[SerializeField] private Renderer[] matRenderers;

    private List<Material> mats = new List<Material>();

	private void Start()
	{
		mats.Clear();

		for (int i = 0; i < matRenderers.Length; i++)
		{
			var m = new Material(matRenderers[i].material);
			mats.Add(m);
			matRenderers[i].material = m; // <-- important
		}
	}

	public void SetMatColor(Color colorToSet)
    {
		foreach (var mat in mats)
			mat.color = colorToSet;
    }
}
