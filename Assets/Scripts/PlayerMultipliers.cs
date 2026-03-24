using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMultipliers : AbstractMultipliers
{
	[Space(5)]

	[SerializeField] private PlayerReferences pRef;

	private float experienceForceFieldBaseSize = 1f;
	private ParticleSystemForceField experienceForceField;

	private void Start()
	{
		if (experienceForceField == null)
			experienceForceField = GetComponentInChildren<ParticleSystemForceField>();
		experienceForceFieldBaseSize = experienceForceField.endRange;
	}
	protected override void ApplyValues()
    {
		pRef.playerDamagable.SetHealthMultiplier();
		experienceForceField.endRange = pRef.playerMultipliers.GetMulti(experienceForceFieldBaseSize, Multiplier.PickupRadius);

		//Set pickup size inside the particle force field
	}
}
