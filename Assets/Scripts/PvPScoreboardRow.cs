using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Threading.Tasks;
using Unity.Collections;

public class PvPScoreboardRow : MonoBehaviour
{
	[SerializeField] private Image avatarImage;
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private TMP_Text killsText;
	[SerializeField] private TMP_Text deathsText;

	private PlayerPvP playerPvP;
	public PlayerPvP Player => playerPvP;

	// ---- LERP FIELDS ----
	private RectTransform rt;
	private Vector2 startPos;
	private Vector2 targetPos;
	private float animTime;
	private bool animating;
	[SerializeField] private float moveDuration = 0.2f;

	private void Awake()
	{
		rt = GetComponent<RectTransform>();
	}

	public void Init(PlayerPvP stats)
	{
		this.playerPvP = stats;

		nameText.text = stats.PlayerName.Value.ToString();
		killsText.text = stats.Kills.Value.ToString();
		deathsText.text = stats.Deaths.Value.ToString();

		stats.Kills.OnValueChanged += OnKillsChanged;
		stats.Deaths.OnValueChanged += OnDeathsChanged;
		stats.PlayerName.OnValueChanged += OnNameChanged;

		_ = LoadAvatarAsync();
	}

	private void OnDestroy()
	{
		if (playerPvP == null) return;

		playerPvP.Kills.OnValueChanged -= OnKillsChanged;
		playerPvP.Deaths.OnValueChanged -= OnDeathsChanged;
		playerPvP.PlayerName.OnValueChanged -= OnNameChanged;
	}

	private void OnKillsChanged(int oldVal, int newVal)
		=> killsText.text = newVal.ToString();

	private void OnDeathsChanged(int oldVal, int newVal)
		=> deathsText.text = newVal.ToString();

	private void OnNameChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
		=> nameText.text = newVal.ToString();

	private async Task LoadAvatarAsync()
	{
		if (!SteamClient.IsValid) return;

		var friend = new Friend((SteamId)playerPvP.SteamId.Value);
		var img = await friend.GetLargeAvatarAsync();  // Facepunch API
		if (!img.HasValue) return;

		var tex = img.Value.Convert();
		var sprite = Sprite.Create(
			tex,
			new Rect(0, 0, tex.width, tex.height),
			new Vector2(0.5f, 0.5f)
		);

		avatarImage.sprite = sprite;
	}

	// ----- LERP API CALLED BY SCOREBOARD -----

	public void CaptureStartPos()
	{
		if (rt == null) rt = GetComponent<RectTransform>();
		startPos = rt.anchoredPosition;
	}

	public void SetTargetToCurrentPos()
	{
		if (rt == null) rt = GetComponent<RectTransform>();
		targetPos = rt.anchoredPosition;
		animTime = 0f;
		animating = true;
	}

	private void Update()
	{
		if (!animating || moveDuration <= 0f) return;

		animTime += Time.unscaledDeltaTime;
		float t = Mathf.Clamp01(animTime / moveDuration);

		// smoothstep-ish curve: ease in & out
		float smoothT = t * t * (3f - 2f * t);

		rt.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, smoothT);


		if (t >= 1f)
			animating = false;
	}
}
