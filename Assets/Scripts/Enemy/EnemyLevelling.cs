using Unity.Netcode;
using UnityEngine;

public class EnemyLevelling : NetworkBehaviour
{
    [SerializeField] private EnemyReferences eRef;

    [Space(3)]

    [SerializeField] private Transform scaleToSize;
    private Vector3 initialSize = Vector3.zero;

	[Space(5)]

    [SerializeField] private int currentLevel = 0;

    [Space(3)]

    [SerializeField] private float dmgMultiplier = 0.1f;
    [SerializeField] private float healthMultiplier = 0.1f;
    [SerializeField] private float speedMultiplier = 0.05f;
    [SerializeField] private float atkspdMultiplier = 0.03f;
    [SerializeField] private float sizeMultiplier = 0.1f;

    [Space(2)]

    [Tooltip("What range a level can be based on the current tier and wave, so wave 1 tier 1 would be between level 1 - 3 if assign range is 3")]
    [SerializeField] private int levelAssignRange = 3;
    [SerializeField] private int extraLevelPerWave = 2;

    [Space(2)]

	[SerializeField] private float extraExpPointsPerLevel = 1f;
	[SerializeField] private float extraExpValuePerLevel = .1f;

	[Space(2)]

    [SerializeField] private float decayPerLevel = .01f;

    [Space(6)]

    [SerializeField] private Color startColor;
    [SerializeField] private Color endColor;

    [Space(4)]

    [SerializeField] private TMPro.TextMeshProUGUI levelText;

	private void Awake()
	{
		if (scaleToSize != null && initialSize == Vector3.zero)
			initialSize = scaleToSize.localScale;
	}

	public override void OnNetworkSpawn()
	{
		if (scaleToSize != null && initialSize == Vector3.zero)
			initialSize = scaleToSize.localScale;
	}

	public void CheckAndAssignLevel(int currentWave, int tierFirstSpawned)
    {
		AssignClientRpc(currentWave, tierFirstSpawned);
    }
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void AssignClientRpc(int currentWave, int tierFirstSpawned)
    {
		if (!NetworkManager.Singleton.IsServer) return;

		if (initialSize == Vector3.zero)
			initialSize = scaleToSize.localScale;

		int extraLevels_Waves = (extraLevelPerWave * currentWave) - (extraLevelPerWave * tierFirstSpawned);
		int extraLevel_Range = Random.Range(1, levelAssignRange + 1);

		int levelToSet = extraLevels_Waves + extraLevel_Range;

		if (levelToSet > 100)
			levelToSet = 100;

		SetLevel(levelToSet);
	}

	private void OnValidate()
	{
        if (Application.isPlaying && Application.isEditor)
			if (NetworkManager.Singleton)
				if (NetworkManager.Singleton.IsServer)
					SetLevel();
	}
	
	private void SetLevel(int lvl = -1)
    {
        if (!NetworkManager.Singleton.IsServer) 
            return;

        SetLevelClientRpc(lvl);
    }
    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void SetLevelClientRpc(int lvl = -1)
    {
		if (scaleToSize != null && initialSize == Vector3.zero)
			initialSize = scaleToSize.localScale;

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

        eRef.experienceEmitter.ChangeEmission(extraExpValuePerLevel * currentLevel, extraExpPointsPerLevel * currentLevel);

        levelText.text = $"Lvl. {currentLevel}";

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
