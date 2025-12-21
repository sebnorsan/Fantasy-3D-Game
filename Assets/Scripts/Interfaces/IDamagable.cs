using UnityEngine;

public interface IDamagable
{
	public void TakeDamage(float amount, Vector3 hitPoint, ArrowEffect[] arrowEffects);
	public void Heal(float amount);
}
