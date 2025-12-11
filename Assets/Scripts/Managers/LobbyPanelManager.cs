using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using EasyTextEffects.Editor.MyBoxCopy.Extensions; // for NetworkManager + IsHost checks

public class LobbyPanelManager : MonoBehaviour
{
	[Header("Main Menu Manager")]
	[SerializeField] private MainMenuManager mainMenuManager;

	[Header("Root")]
	[SerializeField] private GameObject panelRoot;

	[Header("Header UI")]
	[SerializeField] private TMP_Text lobbyNameText;
	[SerializeField] private TMP_Text playerCountText;
	[SerializeField] private TMP_Text localNameText;
	[SerializeField] private UnityEngine.UI.Image localAvatarImage;

	[Header("Buttons")]
	[SerializeField] private Button leaveButton;
	[SerializeField] private Button startGameButton; // host-only
	[SerializeField] private TMP_Dropdown sceneInputField; // host-only
	[SerializeField] private TMP_Text startGameButtonLabel; // optional if you want to set text

	[Header("Members List")]
	[SerializeField] private Transform membersContent;
	[SerializeField] private LobbyMemberRow memberRowPrefab;

	private readonly Dictionary<SteamId, Sprite> avatarCache = new();
	private Lobby? currentLobby;

	private void Awake()
	{
		if (leaveButton != null) leaveButton.onClick.AddListener(OnLeavePressed);
		if (startGameButton != null) startGameButton.onClick.AddListener(OnStartPressed);
		//if (sceneInputField != null) sceneInputField.onEndEdit.AddListener(); 
	}

	private void OnEnable()
	{
		GameNetworkManager.instance.OnLobbyReady += HandleLobbyReady;
		GameNetworkManager.instance.OnLobbyMembersChanged += HandleMembersChanged;
	}

	private void OnDisable()
	{
		if (GameNetworkManager.instance == null) return;
		GameNetworkManager.instance.OnLobbyReady -= HandleLobbyReady;
		GameNetworkManager.instance.OnLobbyMembersChanged -= HandleMembersChanged;
	}

	private async void HandleLobbyReady(Lobby lobby)
	{
		currentLobby = lobby;
		panelRoot.SetActive(true);
		mainMenuManager.OpenLobbyPanel();

		// header
		lobbyNameText.text = lobby.GetData("name");
		playerCountText.text = $"{lobby.MemberCount}/{lobby.MaxMembers}";

		// local player info
		localNameText.text = SteamClient.Name;
		var mySprite = await GetAvatarSpriteAsync(SteamClient.SteamId);
		if (mySprite != null) localAvatarImage.sprite = mySprite;

		// host-only start button visibility
		UpdateHostUIState(lobby);

		// fill members immediately
		await RebuildMembersUI(lobby);
	}

	private async void HandleMembersChanged(Lobby lobby)
	{
		lobby.Refresh();
		await Task.Delay(100);

		if (currentLobby == null || lobby.Id != currentLobby.Value.Id) return;

		playerCountText.text = $"{lobby.MemberCount}/{lobby.MaxMembers}";
		UpdateHostUIState(lobby);

		await RebuildMembersUI(lobby);
	}

	private void UpdateHostUIState(Lobby lobby)
	{
		// NGO host check is the real authority
		bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

		if (startGameButton != null)
			startGameButton.gameObject.SetActive(isHost);

		if (startGameButtonLabel != null && isHost)
			startGameButtonLabel.text = "Start Game";

		if (startGameButton != null)
			sceneInputField.gameObject.SetActive(isHost);
	}

	private async Task RebuildMembersUI(Lobby lobby)
	{
		Debug.Log("Members: " + lobby.MemberCount);
		foreach (var f in lobby.Members)
			Debug.Log(" - " + f.Name + " " + f.Id);

		// clear rows
		for (int i = membersContent.childCount - 1; i >= 0; i--)
			Destroy(membersContent.GetChild(i).gameObject);

		foreach (var friend in lobby.Members)
		{
			var row = Instantiate(memberRowPrefab, membersContent);
			row.SetName(friend.Name);

			var sprite = await GetAvatarSpriteAsync(friend.Id);
			if (sprite != null) row.SetAvatar(sprite);
		}
	}

	private void OnLeavePressed()
	{
		// If host leaves, NGO shuts down and clients get disconnected automatically.
		// If client leaves, they just drop out.
		GameNetworkManager.instance.Disconnect();

		// Hide lobby UI locally; your main menu can re-open itself
		panelRoot.SetActive(false);

		mainMenuManager.BackToMain();
	}

	private void OnStartPressed()
	{
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost) return;

		// Host-only: load your game scene via NGO
		// Replace "GameScene" with your actual scene name.

		int index = sceneInputField.value;

		NetworkManager.Singleton.SceneManager.LoadScene(sceneInputField.options[index].text, UnityEngine.SceneManagement.LoadSceneMode.Single);
	}

	private async Task<Sprite> GetAvatarSpriteAsync(SteamId id)
	{
		if (avatarCache.TryGetValue(id, out var cached))
			return cached;

		try
		{
			var img = await SteamFriends.GetLargeAvatarAsync(id);
			if (!img.HasValue) return null;

			var tex = img.Value.Convert();
			var sprite = Sprite.Create(tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f));

			avatarCache[id] = sprite;
			return sprite;
		}
		catch
		{
			return null;
		}
	}
}

public static class SteamImageExtensions
{
	public static Texture2D Convert(this Steamworks.Data.Image image)
	{
		var tex = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.ARGB32, false);
		tex.filterMode = FilterMode.Trilinear;

		for (int x = 0; x < image.Width; x++)
		{
			for (int y = 0; y < image.Height; y++)
			{
				var p = image.GetPixel(x, y);
				tex.SetPixel(x, (int)image.Height - y,
					new UnityEngine.Color(p.r / 255f, p.g / 255f, p.b / 255f, p.a / 255f));
			}
		}
		tex.Apply();
		return tex;
	}
}
