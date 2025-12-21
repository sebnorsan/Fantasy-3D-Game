using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMultipliers : AbstractMultipliers
{
	[Space(5)]

	[SerializeField] private PlayerReferences pRef;
	
	protected override void ApplyValues()
    {
		pRef.playerDamagable.SetHealthMultiplier();
	}
}
