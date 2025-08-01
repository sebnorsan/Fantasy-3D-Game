using UnityEngine;

public class SkillTree : MonoBehaviour
{
    private PlayerController player;
    private CrystalScript crystal;
    private BowScript bow;
    private void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        crystal = FindFirstObjectByType<CrystalScript>();
        bow = FindFirstObjectByType<BowScript>();
    }

    public void UpgradeArrowSize()
    {
        bow.arrowSize++;
    }
    public void UpgradeCrystalHp()
    {
        crystal.maxHealth += 1000;
        crystal.currentHealth += 1000;
    }
    public void UpgradeDrawSpeed()
    {
        bow.arrowDrawSpeed *= 3;
    }
    public void UpgradeArrowDamage()
    {
        bow.arrowDamage *= 2;
    }
    public void EnableIceArrow()
    {
        bow.arrowEffect.Add(ArrowEffect.Ice);
    }
    public void UpgradeXpGain()
    {
        GameManager.instance.xpMultiplier++;
    }
    public void EnableCrystalRegeneration()
    {
        crystal.Regeneration();
    }
    public void EnableFireArrow()
    {
        bow.arrowEffect.Add(ArrowEffect.Fire);
    }
    public void EnableCrystalThorns()
    {
        crystal.thorns = true;
    }
    public void EnableLightningArrow()
    {
        bow.arrowEffect.Add(ArrowEffect.Lightning);
    }
    public void EnableCrystalDeath()
    {
        crystal.deadly = true;
    }
    public void EnableBombArrows()
    {
        bow.arrowEffect.Add(ArrowEffect.Bomb);
    }
    public void UpgradeLightningChain()
    {
        bow.lightningChain += 3;
    }
}
