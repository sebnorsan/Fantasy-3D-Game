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
}
