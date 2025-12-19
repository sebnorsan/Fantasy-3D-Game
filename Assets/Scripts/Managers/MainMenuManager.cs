using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
	[Header("Main Menu Settings")]
	[SerializeField] private GameObject optionsPanel;      // panel with OptionsManager on it
	[SerializeField] private Button openOptionsButton;     // "Settings" button in main menu
	[SerializeField] private Button optionsBackButton;     // "Back" button on options panel

	[Header("Top Level Menu")]
	[SerializeField] private GameObject topLevelPanel;         // Single / Multi / Settings / Quit
	[SerializeField] private Button singleplayerButton;
	[SerializeField] private Button multiplayerButton;
	[SerializeField] private Button topSettingsButton;
	[SerializeField] private Button topQuitButton;

	[Header("Panels")]
	[SerializeField] private GameObject mainPanel;
	[SerializeField] private GameObject hostPanel;
	[SerializeField] private GameObject browserPanel;
	[SerializeField] private GameObject passwordPanel;

	[Header("Main Buttons")]
	[SerializeField] private Button hostButton;
	[SerializeField] private Button joinButton;
	[SerializeField] private Button mainBackButton; // NEW


	[Header("Host UI")]
	[SerializeField] private TMP_InputField lobbyNameInput;
	[SerializeField] private TMP_InputField lobbyPasswordInput;
	[SerializeField] private TMP_InputField maxPlayersInput; // simple numeric input
	[SerializeField] private Toggle friendsOnlyToggle;
	[SerializeField] private Button startHostButton;
	[SerializeField] private Button hostBackButton;

	[Header("Browser UI")]
	[SerializeField] private Button refreshButton;
	[SerializeField] private Button browserBackButton;
	[SerializeField] private Transform lobbyListContent;
	[SerializeField] private LobbyRowView lobbyRowPrefab;
	[SerializeField] private TMP_Text browserStatusText;

	[Header("Password UI")]
	[SerializeField] private TMP_InputField joinPasswordInput;
	[SerializeField] private Button passwordJoinButton;
	[SerializeField] private Button passwordCancelButton;
	[SerializeField] private TMP_Text passwordTitleText;

	private Lobby pendingJoinLobby;
	private bool isRefreshing;
	private bool isJoining;

	private GameObject lastActivePanel;

	private void Start()
	{
		hostButton.onClick.AddListener(OpenHostPanel);
		joinButton.onClick.AddListener(OpenBrowserPanel);

		if (mainBackButton != null)
			mainBackButton.onClick.AddListener(ShowTopLevelMenu);

		startHostButton.onClick.AddListener(OnStartHostClicked);
		hostBackButton.onClick.AddListener(BackToMain);

		refreshButton.onClick.AddListener(() => _ = RefreshLobbies());
		browserBackButton.onClick.AddListener(BackToMain);

		passwordJoinButton.onClick.AddListener(() => _ = ConfirmPasswordJoin());
		passwordCancelButton.onClick.AddListener(ClosePasswordPanel);

		if (singleplayerButton != null)
			singleplayerButton.onClick.AddListener(OnSingleplayerClicked);

		if (multiplayerButton != null)
			multiplayerButton.onClick.AddListener(OpenMultiplayerMain);

		// TOP LEVEL "Settings" button
		if (topSettingsButton != null)
			topSettingsButton.onClick.AddListener(OpenOptionsPanel);

		if (topQuitButton != null)
			topQuitButton.onClick.AddListener(OnTopQuitClicked);

		// GLOBAL COG button
		if (optionsPanel != null)
			optionsPanel.SetActive(false);

		if (openOptionsButton != null)
			openOptionsButton.onClick.AddListener(OpenOptionsPanel);

		if (optionsBackButton != null)
			optionsBackButton.onClick.AddListener(CloseOptionsPanel);

		ShowTopLevelMenu();
	}
	private void ShowTopLevelMenu()
	{
		if (topLevelPanel != null)
			topLevelPanel.SetActive(true);

		mainPanel.SetActive(false);
		hostPanel.SetActive(false);
		browserPanel.SetActive(false);
		passwordPanel.SetActive(false);
	}
	private void OpenMultiplayerMain()
	{
		if (topLevelPanel != null)
			topLevelPanel.SetActive(false);

		mainPanel.SetActive(true);
		hostPanel.SetActive(false);
		browserPanel.SetActive(false);
		passwordPanel.SetActive(false);
	}
	private void OpenOptionsPanel()
	{
		if (optionsPanel == null)
			return;

		if (optionsPanel.activeSelf)
			return;

		if (openOptionsButton != null) openOptionsButton.interactable = false;
		if (topSettingsButton != null) topSettingsButton.interactable = false;

		lastActivePanel = null;

		if (topLevelPanel != null && topLevelPanel.activeSelf)
			lastActivePanel = topLevelPanel;
		else if (mainPanel != null && mainPanel.activeSelf)
			lastActivePanel = mainPanel;
		else if (hostPanel != null && hostPanel.activeSelf)
			lastActivePanel = hostPanel;
		else if (browserPanel != null && browserPanel.activeSelf)
			lastActivePanel = browserPanel;
		else if (passwordPanel != null && passwordPanel.activeSelf)
			lastActivePanel = passwordPanel;

		optionsPanel.SetActive(true);

		if (topLevelPanel != null) topLevelPanel.SetActive(false);
		if (mainPanel != null) mainPanel.SetActive(false);
		if (hostPanel != null) hostPanel.SetActive(false);
		if (browserPanel != null) browserPanel.SetActive(false);
		if (passwordPanel != null) passwordPanel.SetActive(false);
	}

	private void CloseOptionsPanel()
	{
		if (optionsPanel != null)
			optionsPanel.SetActive(false);

		if (openOptionsButton != null) openOptionsButton.interactable = true;
		if (topSettingsButton != null) topSettingsButton.interactable = true;

		if (lastActivePanel != null)
			lastActivePanel.SetActive(true);
		else
			ShowTopLevelMenu();
	}


	public void BackToMain()
	{
		if (topLevelPanel != null)
			topLevelPanel.SetActive(false);

		mainPanel.SetActive(true);
		hostPanel.SetActive(false);
		browserPanel.SetActive(false);
		passwordPanel.SetActive(false);
	}

	public void OpenHostPanel()
	{
		mainPanel.SetActive(false);
		hostPanel.SetActive(true);
		browserPanel.SetActive(false);
		passwordPanel.SetActive(false);
	}

	public void OpenBrowserPanel()
	{
		mainPanel.SetActive(false);
		hostPanel.SetActive(false);
		browserPanel.SetActive(true);
		passwordPanel.SetActive(false);

		_ = RefreshLobbies();
	}
	public void OpenLobbyPanel()
	{
		mainPanel.SetActive(false);
		hostPanel.SetActive(false);
		browserPanel.SetActive(false);
		passwordPanel.SetActive(false);
	}
	private void OnSingleplayerClicked()
	{
		if (NetworkManager.Singleton == null)
		{
			Debug.LogError("No NetworkManager in scene for Singleplayer.");
			return;
		}

		// Make sure host is started once
		if (!NetworkManager.Singleton.IsListening)
		{
			bool ok = NetworkManager.Singleton.StartHost();
			if (!ok)
			{
				Debug.LogError("Failed to start singleplayer host.");
				return;
			}
		}

		// IMPORTANT: use NGO scene manager so NetworkBehaviours (like GameSceneSpawnManager)
		// get their OnNetworkSpawn called
		NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
	}

	private void OnTopQuitClicked()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
	Application.Quit();
#endif
	}

	private void OnStartHostClicked()
	{
		var name = lobbyNameInput.text;
		var pw = lobbyPasswordInput.text;

		int maxPlayers = 10;
		if (!int.TryParse(maxPlayersInput.text, out maxPlayers))
			maxPlayers = 10;

		bool friendsOnly = friendsOnlyToggle != null && friendsOnlyToggle.isOn;

		GameNetworkManager.instance.StartHost(name, pw, maxPlayers, friendsOnly);
	}

	private async Task RefreshLobbies()
	{
		if (isRefreshing) return;
		isRefreshing = true;

		browserStatusText.text = "Refreshing...";
		ClearLobbyRows();

		var lobbies = await GameNetworkManager.instance.RefreshLobbiesAsync();

		if (lobbies.Length == 0)
		{
			browserStatusText.text = "No lobbies found.";
			isRefreshing = false;
			return;
		}

		browserStatusText.text = "";

		foreach (var lobby in lobbies)
		{
			var row = Instantiate(lobbyRowPrefab, lobbyListContent);

			var name = lobby.GetData("name");
			if (string.IsNullOrEmpty(name)) name = "Lobby";

			bool passworded = !string.IsNullOrEmpty(lobby.GetData("pwHash"));

			row.Bind(
				lobby,
				name,
				lobby.MemberCount,
				lobby.MaxMembers,
				passworded,
				OnJoinLobbyPressed
			);
		}

		isRefreshing = false;
	}

	private void ClearLobbyRows()
	{
		for (int i = lobbyListContent.childCount - 1; i >= 0; i--)
			Destroy(lobbyListContent.GetChild(i).gameObject);
	}

	private void OnJoinLobbyPressed(Lobby lobby, bool passworded)
	{
		if (isJoining) return;

		if (passworded)
		{
			pendingJoinLobby = lobby;
			passwordTitleText.text = $"Join {lobby.GetData("name")}";
			joinPasswordInput.text = "";
			passwordPanel.SetActive(true);
		}
		else
		{
			_ = JoinLobbyDirect(lobby, "");
		}
	}

	private async Task ConfirmPasswordJoin()
	{
		if (isJoining) return;
		ClosePasswordPanel();

		await JoinLobbyDirect(pendingJoinLobby, joinPasswordInput.text);
	}

	private async Task JoinLobbyDirect(Lobby lobby, string passwordAttempt)
	{
		isJoining = true;
		browserStatusText.text = "Joining...";

		bool ok = await GameNetworkManager.instance.TryJoinLobbyAsync(lobby, passwordAttempt);

		if (!ok)
		{
			browserStatusText.text = "Wrong password or failed to join.";
			isJoining = false;
			Invoke(nameof(ResetStatusText), 3f);
			return;
		}

		browserStatusText.text = "Joined! Connecting...";
		// OnLobbyEntered callback will start the NGO client.
		isJoining = false;
	}
	private void ResetStatusText()
	{
		browserStatusText.text = "";
	}
	private void ClosePasswordPanel()
	{
		passwordPanel.SetActive(false);
	}
}
