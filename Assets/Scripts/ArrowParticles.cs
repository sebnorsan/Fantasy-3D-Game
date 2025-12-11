using UnityEngine;

public class ArrowParticles : MonoBehaviour
{
    [SerializeField] private PlayerReferences pRef;

    [Header("Particles")]

    [SerializeField] private ParticleSystem ArrowFirePfx;

	public void ApplyArrowEffects()
	{
		foreach (var effect in pRef.bowEffects.activeArrowEffects.Value)
		{
			switch (effect)
			{
				case ArrowEffect.BigHit:
					EffectHelper(ArrowFirePfx, true);
					break;
				default:
					break;
			}
		}
	}
	public void UnApplyArrowEffects()
	{
		foreach (var effect in pRef.bowEffects.activeArrowEffects.Value)
		{
			switch (effect)
			{
				case ArrowEffect.BigHit:
					EffectHelper(ArrowFirePfx, false);
					break;
				default:
					break;
			}
		}
	}
	private void EffectHelper(ParticleSystem pfx, bool play)
	{
		var main = pfx.main;

		if (play)
		{
			main.playOnAwake = true;
			pfx.Play();
		}
		else
		{
			main.playOnAwake = false;
			pfx.Stop();
		}
	}
}
