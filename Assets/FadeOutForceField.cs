using UnityEngine;

public class FadeOutForceField : MonoBehaviour
{
	[Range(0, 1)]
	public float alpha = 1;

	private Material material;

	private void Start()
	{
		// Get the material from the renderer
		Renderer renderer = GetComponent<Renderer>();
		if (renderer != null)
		{
			material = renderer.material; // This instantiates a unique copy for this GameObject
		}
	}

	private void Update()
	{
		if (material != null)
		{
			// Update alpha for each color property
			UpdateColorAlpha("Color_ae4adae7db6b40efa0ef19bca693ae89");
			UpdateColorAlpha("Color_e988ca55ce2a4900b2e46dc6e5f7b68e");
			UpdateColorAlpha("Color_691887cf5cd946bfa6c9cb20e8658dfc");
		}
	}

	private void UpdateColorAlpha(string propertyName)
	{
		if (material.HasProperty(propertyName))
		{
			Color color = material.GetColor(propertyName);
			color.a = alpha;
			material.SetColor(propertyName, color);
		}
	}
}
