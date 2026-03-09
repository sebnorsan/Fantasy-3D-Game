using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMultipliers : AbstractMultipliers
{
	[Space(5)]

	[SerializeField] private int extraJump = 0;

	[Space(5)]

	[SerializeField] private PlayerReferences pRef;

	public int GetExtraJumps() => extraJump;

	protected override void ApplyValues()
    {
		pRef.playerDamagable.SetHealthMultiplier();
		//Set pickup size inside the particle force field
	}
}
