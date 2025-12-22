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

        eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.Damage, dmgMultiplier * currentLevel);
        eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.Health, healthMultiplier * currentLevel);
        eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.Speed, speedMultiplier * currentLevel);
        eRef.enemyMultipliers.SetPermanentMultiplier(Multiplier.AtkSpd, atkspdMultiplier * currentLevel);

        SetSize();
    }
    private void SetSize()
    {
        scaleToSize.localScale = initialSize * (1 + (sizeMultiplier * currentLevel));
    }
}
