using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class BowEffects : NetworkBehaviour
{
    public PlayerReferences pRef;

    [Space(10)]

    public NetworkVariable<List<ArrowEffect>> activeArrowEffects = 
        new NetworkVariable<List<ArrowEffect>>(null,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Owner);

    public void UpdateEffects()
    {
        //List<ArrowEffect> effectsToDisable = new List<ArrowEffect>();

        //foreach (var effect in activeArrowEffects.Value)
        //{
        //    switch (effect)
        //    {
        //        case ArrowEffect.BigHit:
        //            break;
        //        default:
        //            break;
        //    }
        //}

        if (GetBigHit())
		    SetBigHit(false);

		foreach (var fx in pRef.arrowParticles)
			fx.UnApplyArrowEffects();
	}

	public void SetBigHit(bool b)
    {
		if (!IsServer && !IsOwner) return;

        if (b)
            if (GetBigHit()) return;

		SetHelper(b, ArrowEffect.BigHit);
		pRef.playerGraphics.PlayParticle(PfxToPlay.Fire, b);
	}
	public bool GetBigHit()
	{
		return GetHelper(ArrowEffect.BigHit);
	}
	#region Helpers
	private bool GetHelper(ArrowEffect effect)
    {
		if (activeArrowEffects == null)
			activeArrowEffects.Value = new List<ArrowEffect>();

        if (activeArrowEffects.Value.Contains(effect))
            return true;

        return false;
	}
    private void SetHelper(bool b, ArrowEffect effect)
    {
        if (activeArrowEffects == null)
            activeArrowEffects.Value = new List<ArrowEffect>();

        if (b)
            activeArrowEffects.Value.Add(effect);
        else
			activeArrowEffects.Value.Remove(effect);

        foreach (var fx in pRef.arrowParticles)
            fx.ApplyArrowEffects();
	}
	#endregion
}
public enum ArrowEffect
{
    BigHit
}