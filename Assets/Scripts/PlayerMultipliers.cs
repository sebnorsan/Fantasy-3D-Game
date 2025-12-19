using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMultipliers : MonoBehaviour
{
    [SerializeField] private PlayerReferences pRef;

    [Space(5)]

    [SerializeField] private float damageMultiplier = 100;
    [SerializeField] private float healthMultiplier = 100;
    [SerializeField] private float speedMultiplier = 100;
    [SerializeField] private float jumpMultiplier = 100;

    public float GetDamageMulti(float baseVal) => baseVal * (damageMultiplier / 100);
    public float GetHealthMulti(float baseVal) => baseVal * (healthMultiplier / 100);
    public float GetSpeedMulti(float baseVal) => baseVal * (speedMultiplier / 100);
    public float GetJumpMulti(float baseVal) => baseVal * (jumpMultiplier / 100);


    private List<Coroutine> currentTempMults = new List<Coroutine>();

    private void ApplyValuesToPlayer()
    {
		pRef.playerDamagable.SetHealthMultiplier();
	}
	public void ApplyPermanentMultiplier(PlayerMultiplier type, float percent)
	{
		AddMultiplier(type, percent);
	}
    public void ApplyTemporaryMultiplier(PlayerMultiplier type, float percent, float resetTime)
    {
        AddMultiplier(type, percent);
		currentTempMults.Add(StartCoroutine(TempMultiplierRemover(type, percent, resetTime)));
    }
    private IEnumerator TempMultiplierRemover(PlayerMultiplier type, float amount, float resetTime)
    {
        yield return new WaitForSeconds(resetTime);

        RemoveMultiplier(type, amount);
    }
    private void AddMultiplier(PlayerMultiplier type, float percent)
    {
        switch (type)
        {
            case PlayerMultiplier.Damage:
                damageMultiplier += percent;
                break;
            case PlayerMultiplier.Health:
                healthMultiplier += percent;
				break;
            case PlayerMultiplier.Speed:
                speedMultiplier += percent;
                break;
            case PlayerMultiplier.Jump:
                jumpMultiplier += percent;
                break;
            default:
                break;
        }

		ApplyValuesToPlayer();
	}
    private void RemoveMultiplier(PlayerMultiplier type, float percent)
    {
		switch (type)
		{
			case PlayerMultiplier.Damage:
				damageMultiplier -= percent;
				damageMultiplier = Mathf.Max(1f, damageMultiplier);
				break;
			case PlayerMultiplier.Health:
				healthMultiplier -= percent;
				healthMultiplier = Mathf.Max(1f, healthMultiplier);
				break;
			case PlayerMultiplier.Speed:
				speedMultiplier -= percent;
				speedMultiplier = Mathf.Max(1f, speedMultiplier);
				break;
			case PlayerMultiplier.Jump:
				jumpMultiplier -= percent;
				jumpMultiplier = Mathf.Max(1f, jumpMultiplier);
				break;
			default:
				break;
		}

        ApplyValuesToPlayer();
	}
}
public enum PlayerMultiplier
{
    Damage,
    Health,
    Speed,
    Jump
}