using UnityEngine;
using EZCameraShake;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    [Header("Damage & Effects")]
    public int arrowDamage = 1;
    public ArrowEffect[] arrowEffect;
    public int lightningChain = 3;

    [Header("Flight Settings")]
    public float initialSpeed = 30f;   // How fast the arrow is shot
    public float dropGravity = 1f;     // How strong the downward pull is
    public float lifeTime = 10f;       // Destroy after this many seconds

    public bool cutTrees = false;

    // Correct for a mesh that needs a 90° pitch to face its Z+ forward
    private readonly Quaternion modelCorrection = Quaternion.Euler(90f, 0f, 0f);

    private Vector3 initPlayerPos;
    private Vector3 velocity;
    private Rigidbody rb;

    // lightning specific
    private Transform lightningTarget;

    public GameObject explosion;

    private void Start()
    {
        var bowComponent = FindFirstObjectByType<BowScript>();
        arrowDamage = bowComponent.arrowDamage;
        arrowEffect = bowComponent.arrowEffect.ToArray();
        initialSpeed = bowComponent.arrowSpeed;
        cutTrees = bowComponent.arrowCutsTrees;
        lightningChain = bowComponent.lightningChain;

        transform.localScale = transform.localScale *= bowComponent.arrowSize;

        initPlayerPos = FindFirstObjectByType<PlayerController>().transform.position;

        CameraShaker.Instance.ShakeOnce(2f, 3f, .1f, .2f);

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // We’ll apply our own “gravity”

        // 1) Compute aim direction from screen center
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        Vector3 shootDir = ray.direction.normalized;

        // 2) Set initial velocity
        velocity = shootDir * initialSpeed;

        // 3) Orient arrow (with model correction)
        transform.rotation = Quaternion.LookRotation(shootDir) * modelCorrection;

        // 4) Auto‑destroy to clean up
        Destroy(gameObject, lifeTime);

        Invoke(nameof(EnableTrailAfterDelay), 0.03f);
    }

    private void EnableTrailAfterDelay() => GetComponentInChildren<TrailRenderer>().enabled = true;

    void FixedUpdate()
    {
        if (lightningTarget != null)
        {
            // Fly directly at lightning target
            Vector3 dir = (lightningTarget.position - rb.position).normalized;
            velocity = dir * initialSpeed; // reset velocity, ignore drop
            Vector3 newPos = rb.position + velocity * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            rb.MoveRotation(Quaternion.LookRotation(dir) * modelCorrection);
        }
        else
        {
            // Normal arrow physics
            Vector3 newPos = rb.position + velocity * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
            velocity += Vector3.down * dropGravity * Time.fixedDeltaTime;

            if (velocity.sqrMagnitude > 0.001f)
            {
                Quaternion aimRot = Quaternion.LookRotation(velocity.normalized);
                rb.MoveRotation(aimRot * modelCorrection);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Tree")
        {
            if (!cutTrees) return;

            if (!collision.gameObject.TryGetComponent(out KillableObject killable))
            {
                var newComp = collision.gameObject.AddComponent<KillableObject>();
                newComp.maxHealth = 5;
                newComp.hitParticles = Resources.Load<GameObject>("PFX/HitFX (big)");
                newComp.deathParticles = Resources.Load<GameObject>("PFX/Explosion Tree");
                newComp.currentHealth = newComp.maxHealth;
                newComp.parentHitFx = false;
            }
        }

        if (collision.gameObject.TryGetComponent(out IDamagable component))
        {
            var enem = collision.gameObject.GetComponent<AbstractEnemy>();

            foreach (var effect in arrowEffect)
            {
                switch (effect)
                {
                    case ArrowEffect.Fire:
                        enem.FireEffect();
                        break;
                    case ArrowEffect.Ice:
                        enem.IceEffect();
                        break;
                    case ArrowEffect.Lightning:
                        HandleLightningChain(collision.transform);
                        break;
                    case ArrowEffect.Bomb:
                        var expl = Instantiate(explosion, transform.position, explosion.transform.rotation);
                        Destroy(expl, 5f);
                        break;
                }
            }

            component.TakeDamage(arrowDamage);
            component.DamageEffects(initPlayerPos);
        }
    }

    private void HandleLightningChain(Transform hitEnemy)
    {
        if (lightningChain > 0)
        {
            Transform nextTarget = FindNextEnemy(hitEnemy);
            if (nextTarget != null)
            {
                lightningTarget = nextTarget;
                lightningChain--;
            }
            else
            {
                Destroy(gameObject, 0.05f); // no valid next target
            }
        }
        else
        {
            lightningTarget = null;
            Destroy(gameObject, 0.05f);
        }
    }

    private Transform FindNextEnemy(Transform exclude)
    {
        float searchRadius = 20f; // adjust to taste
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);

        foreach (var hit in hits)
        {
            if (hit.transform == exclude) continue; // don’t chain to same enemy
            if (hit.TryGetComponent(out IDamagable dmg))
            {
                return hit.transform;
            }
        }
        return null;
    }
}

public enum ArrowEffect
{
    Normal,
    Ice,
    Lightning,
    Bomb,
    Fire
}
