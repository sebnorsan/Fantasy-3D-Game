using UnityEngine;
using System.Collections;
public class ChangeLightingEvent : AbstractEvent
{
	public float changedDensity;
	public Color fogColor;

	[Space(15)]

	public bool skyboxIsNull = false;
	public Material skyboxMaterial;
	public Color viewportBackgroundColor;
	public override void CallEvent()
	{
		// Density + fog color
		if (changedDensity > -1f)
		{
			if (fogColor.a > 0f)
				EventManager.instance.CallDensityChange(changedDensity, fogColor);
			else
				EventManager.instance.CallDensityChange(changedDensity);
		}

		// Skybox / viewport background
		if (skyboxIsNull || skyboxMaterial != null)
		{
			if (skyboxIsNull)
				EventManager.instance.CallSkyboxOff();
			else
				EventManager.instance.CallSkyboxOn();

			if (viewportBackgroundColor.a > 0f)
				EventManager.instance.CallSkyboxChange(skyboxMaterial, viewportBackgroundColor);
			else
				EventManager.instance.CallSkyboxChange(skyboxMaterial);
		}
	}

}
