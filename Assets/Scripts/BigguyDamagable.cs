using UnityEngine;

public class BigguyDamagable : MonoBehaviour
{
    public GameObject damagePfx;
    public void TakeDamage(Transform t, int dmg)
    {
        var pfx = Instantiate(damagePfx, t.position, Quaternion.identity);
        GetComponentInParent<KillableObject>().TakeDamage(dmg);
        if (GetComponentInParent<KillableObject>().currentHealth <= 0)
        {
			GetComponentInParent<Animator>().SetTrigger("Win");
			EnemySpawnerManager.instance.StopAllCoroutines();
			Destroy(EnemySpawnerManager.instance.gameObject);
            var allenemeis = FindObjectsByType<AbstractEnemy>(FindObjectsSortMode.None);
            foreach (var enemy in allenemeis)
            {
                Destroy(enemy.gameObject);
            }
		}
        else
        {
			GetComponentInParent<Animator>().SetTrigger("Damaged");
		}
	}
}
