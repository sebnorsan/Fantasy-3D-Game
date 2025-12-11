using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class Explosion : MonoBehaviour
{
    private SphereCollider sphere;
    public int explosionDamage = 10;

    private void Awake()
    {
        sphere = GetComponent<SphereCollider>();
        if (!sphere.isTrigger)
        {
            Debug.LogWarning("Explosion SphereCollider should be set to Trigger for detection.");
        }

        // Find all enemies inside the sphere
        List<AbstractEnemy> enemies = FindEnemiesInSphere();

        foreach (var enemy in enemies)
        {
            enemy.TakeDamage(explosionDamage, enemy.transform.position, null);
        }
    }

    private List<AbstractEnemy> FindEnemiesInSphere()
    {
        List<AbstractEnemy> results = new List<AbstractEnemy>();

        Collider[] hits = Physics.OverlapSphere(transform.position, sphere.radius * transform.localScale.x);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out AbstractEnemy enemy))
            {
                results.Add(enemy);
            }
        }

        return results;
    }
}
