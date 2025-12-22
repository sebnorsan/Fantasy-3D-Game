using UnityEngine;

public class EnemyLevelling : MonoBehaviour
{
    [SerializeField] private EnemyReferences eRef;

    [Space(3)]

    [SerializeField] private Transform scaleToSize;
    private Vector3 initialSize;

	[Space(5)]

    [SerializeField] private int currentLevel = 0;

    [Space(3)]

    [SerializeField] private float dmgMultiplier = 0.1f;
    [SerializeField] private float healthMultiplier = 0.1f;
    [SerializeField] private float speedMultiplier = 0.05f;
    [SerializeField] private float atkspdMultiplier = 0.03f;
    [SerializeField] private float sizeMultiplier = 0.1f;

    [Space(2)]

    [SerializeField] private float decayPerLevel = .01f;

    [Space(6)]

    [SerializeField] private Color startColor;
    [SerializeField] private Color endColor;

	private void OnValidate()
	{
        if (Application.isPlaying && Application.isEditor)
            SetLevel();
	}
	private void Start()
	{
        initialSize = scaleToSize.localScale;
	}
	private void SetLevel(int lvl = -1)
    {
        if (lvl != -1)
            currentLevel = lvl;
        if (currentLevel > 100)
        {
			currentLevel = 100;
            return;
		}

		float decay = Mathf.Pow(1f - decayPerLevel, currentLevel);
		float levelFactor = currentLevel * decay;

		eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.Damage, dmgMultiplier * levelFactor);
        eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.Health, healthMultiplier * levelFactor);
        eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.Speed, speedMultiplier * levelFactor);
        eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.AtkSpd, atkspdMultiplier * levelFactor);

        SetColor();
        SetSize(levelFactor);
    }
    private void SetSize(float levelFactor)
    {
        scaleToSize.localScale = initialSize * (1 + (sizeMultiplier * levelFactor));
    }
    private void SetColor()
    {
		float t = Mathf.InverseLerp(0f, 100f, currentLevel);
		Color c = Color.Lerp(startColor, endColor, t);
		eRef.enemyGraphics.SetMatColor(c);
	}
}
