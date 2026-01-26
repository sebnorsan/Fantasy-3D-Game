using UnityEngine;

public interface IDamagable
{
	public void TakeDamage(float amount, Vector3 hitPoint, ArrowEffect[] arrowEffects, float knockbackMultiplier);
	public void Heal(float amount);
}
