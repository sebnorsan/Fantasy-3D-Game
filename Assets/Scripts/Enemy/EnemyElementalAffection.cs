using System.Collections;
using System.Net.NetworkInformation;
using Unity.Netcode;
using UnityEngine;

public class EnemyElementalAffection : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[SerializeField] private ParticleSystem firePfx;
	[SerializeField] private ParticleSystem icePfx;

	public enum ElementPfxToPlay
	{
		Fire,
		Ice
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		firePfx.Stop();
		icePfx.Stop();
	}

	#region Fire

	private Coroutine fireDamageTick;
	private void SetFire(ulong shooterClientId, PlayerReferences pRef)
	{
		if (fireDamageTick != null)
			StopCoroutine(fireDamageTick);
		fireDamageTick = StartCoroutine(DoDamageTick(
			ElementPfxToPlay.Fire, 
			ArrowEffect.Fire, 
			pRef.playerMultipliers.GetMulti(pRef.playerPermanents.GetFlatMultiplier(FlatMultiplier.BurnDamage), Multiplier.BurnDamage), 
			1,
			pRef.playerMultipliers.GetMulti(pRef.playerPermanents.GetFlatMultiplier(FlatMultiplier.BurnTime), Multiplier.BurnTime), shooterClientId));
	}

	#endregion

	#region Ice

	#endregion

	#region Lightning

	#endregion

	#region Helpers

	public void ApplyElements(ArrowEffect[] arrowEffects, ulong shooterClientId, PlayerReferences pRef)
	{
		if (arrowEffects == null) return;

		foreach (var effect in arrowEffects)
		{
			switch (effect)
			{
				case ArrowEffect.BigHit:
					break;
				case ArrowEffect.Critical:
					break;
				case ArrowEffect.Fire:
					//if (pRef.playerMultipliers.RollChance(Chance.BurnChance))
					//	SetFire(shooterClientId, pRef);
					break;
				case ArrowEffect.Ice:
					break;
				case ArrowEffect.Lightning:
					break;
				default:
					break;
			}
		}

		//Chances

		if (pRef.playerMultipliers.RollChance(Chance.BurnChance))
			SetFire(shooterClientId, pRef);
	}

	private void DoPfx(ElementPfxToPlay pfxEnum, bool play)
	{
		ulong predictorClientId = NetworkManager.Singleton.LocalClientId;

		PlayPfxServerRpc(pfxEnum, play, predictorClientId);
		PlayPfxAction(pfxEnum, play);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void PlayPfxServerRpc(ElementPfxToPlay pfxEnum, bool play, ulong predictorClientId)
	{
		PlayPfxClientRpc(pfxEnum, play, predictorClientId);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void PlayPfxClientRpc(ElementPfxToPlay pfxEnum, bool play, ulong predictorClientId)
	{
		if (NetworkManager.Singleton.LocalClientId == predictorClientId) return;

		PlayPfxAction(pfxEnum, play);
	}
	private void PlayPfxAction(ElementPfxToPlay pfxEnum, bool play)
	{
		ParticleSystem pfx = null;

		switch (pfxEnum)
		{
			case ElementPfxToPlay.Fire:
				pfx = firePfx;
				break;
			case ElementPfxToPlay.Ice:
				pfx = icePfx;
				break;
			default:
				return;
		}

		if (play)
			pfx.Play();
		else
			pfx.Stop();
	}

	private IEnumerator DoDamageTick(ElementPfxToPlay pfxToPlay, ArrowEffect effect, float damage, float timeBetweenDamage, float affectedTime, ulong shooterClientId)
	{
		DoPfx(pfxToPlay, true);

		float timer = 0f;

		while (timer < affectedTime)
		{
			yield return new WaitForSeconds(timeBetweenDamage);

			timer += timeBetweenDamage;
			if (timer > affectedTime) yield break;

			DoDamageServerRpc(damage, shooterClientId);
		}

		DoPfx(pfxToPlay, false);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void DoDamageServerRpc(float damage, ulong shooterClientId)
	{
		eRef.enemyDamagable.SetLastHitBy(shooterClientId);
		eRef.enemyDamagable.TakeDamage(damage, Vector3.zero, null, 0, false);
	}
	#endregion
}
