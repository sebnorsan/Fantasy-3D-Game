using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Net.NetworkInformation;

public class GameNetworkManager : MonoBehaviour
{
	public static GameNetworkManager instance = null;

	public Lobby? currLobby { get; private set; } = null;

	private FacepunchTransport transport = null;

	// host settings cached until OnLobbyCreated fires
	private string pendingLobbyName = "Lobby";
	private string pendingPwHash = "";
	private int pendingMaxMembers = 4;
	private bool pendingFriendsOnly = false;

	[SerializeField] private string gameVersion = "0.1"; // set this per build

	public event Action<Lobby> OnLobbyReady;
	public event Action<Lobby> OnLobbyMembersChanged;

	[SerializeField] private string mainMenuSceneName = "LobbyScene"; // set in inspector
	public static string LastNetworkErrorMessage { get; private set; }

	private void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}
		instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		transport = GetComponent<FacepunchTransport>();

		SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
		SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
		SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
		SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
		SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;

		SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

		if (SteamClient.IsValid)
			SteamNetworkingUtils.InitRelayNetworkAccess();
	}

	private void OnDestroy()
	{
		SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
		SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
		SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
		SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
		SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;

		SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

		if (NetworkManager.Singleton == null) return;

		NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
		NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
	}

	// ------------------ Public API ------------------

	public async void StartHost(string lobbyName, string lobbyPassword, int maxMembers, bool friendsOnly = true)
	{
		if (!SteamClient.IsValid)
		{
			Debug.LogError("Steam is not valid.");
			return;
		}

		pendingLobbyName = string.IsNullOrWhiteSpace(lobbyName) ? "Lobby" : lobbyName.Trim();
		pendingPwHash = string.IsNullOrEmpty(lobbyPassword) ? "" : HashPassword(lobbyPassword);
		pendingMaxMembers = Mathf.Clamp(maxMembers, 1, 100);
		pendingFriendsOnly = friendsOnly;

		NetworkManager.Singleton.OnServerStarted += OnServerStarted;
		NetworkManager.Singleton.StartHost();

		await SteamMatchmaking.CreateLobbyAsync(pendingMaxMembers);
		// OnLobbyCreated will fire after this
	}

	public void StartClient(SteamId hostId)
	{
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

		transport.targetSteamId = hostId;

		bool ok = NetworkManager.Singleton.StartClient();
		Debug.Log($"StartClient to {hostId} returned {ok}");
	}

	public async Task<Lobby[]> RefreshLobbiesAsync()
	{
		if (!SteamClient.IsValid)
		{
			Debug.LogError("Steam is not valid.");
			return null;
		}

		var query = SteamMatchmaking.LobbyList;
		query.WithSlotsAvailable(1);
		query.WithKeyValue("version", gameVersion);

		var lobbies = await query.RequestAsync(); // Facepunch lobby browser call
		if (lobbies == null) return Array.Empty<Lobby>();

		// Ensure metadata is available before GetData()
		foreach (var l in lobbies)
			l.Refresh(); // async metadata pull
		await Task.Delay(200); // small wait for LobbyDataUpdate_t

		return lobbies;
	}

	public async Task<bool> TryJoinLobbyAsync(Lobby lobby, string passwordAttempt)
	{
		if (!SteamClient.IsValid)
		{
			Debug.LogError("Steam is not valid.");
			return false;
		}

		lobby.Refresh();
		await Task.Delay(150);

		var pwHash = lobby.GetData("pwHash");
		if (!string.IsNullOrEmpty(pwHash))
		{
			var attemptHash = HashPassword(passwordAttempt ?? "");
			if (!string.Equals(attemptHash, pwHash, StringComparison.Ordinal))
				return false; // wrong password
		}

		var enterResult = await lobby.Join();
		return enterResult == RoomEnter.Success;
	}

	public void Disconnect()
	{
		currLobby?.Leave();
		currLobby = null;

		if (NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.Shutdown();
	}

	private void OnApplicationQuit() => Disconnect();

	// ------------------ NGO callbacks ------------------

	private void OnServerStarted() => Debug.Log("Server has started!", this);
	private void OnClientConnectedCallback(ulong clientId) => Debug.Log($"Client connected, clientId={clientId}");
	private void OnClientDisconnectCallback(ulong clientId)
	{
		Debug.Log($"Client disconnected, clientId={clientId}");

		// If we're a client (not server/host) and *we* got disconnected -> leave lobby + back to menu
		if (!NetworkManager.Singleton.IsServer &&
			clientId == NetworkManager.Singleton.LocalClientId)
		{
			LastNetworkErrorMessage = "Disconnected from host.";

			// Leave Steam lobby + shut down NGO
			Disconnect();   // this already does currLobby?.Leave() and NetworkManager.Singleton.Shutdown()

			// Load menu locally
			SceneManager.LoadScene(mainMenuSceneName);
		}
	}


	// ------------------ Steam callbacks ------------------

	private void OnLobbyEntered(Lobby lobby)
	{
		if (NetworkManager.Singleton.IsHost) return;
		StartClient(lobby.Owner.Id);

		currLobby = lobby;

		OnLobbyReady?.Invoke(lobby);
		OnLobbyMembersChanged?.Invoke(lobby);
	}

	private void OnLobbyCreated(Result result, Lobby lobby)
	{
		if (result != Result.OK)
		{
			Debug.LogError($"Lobby couldn't be created: {result}", this);
			return;
		}

		// Facepunch creates lobbies invisible by default.
		if (pendingFriendsOnly) 
			lobby.SetFriendsOnly();
		else 
			lobby.SetPublic(); // make it visible in browser

		lobby.SetJoinable(true);
		lobby.MaxMembers = pendingMaxMembers;

		lobby.SetData("name", pendingLobbyName);
		lobby.SetData("pwHash", pendingPwHash);
		lobby.SetData("version", gameVersion);

		Debug.Log($"Lobby created: {pendingLobbyName} ({lobby.MemberCount}/{lobby.MaxMembers})", this);

		currLobby = lobby;

		OnLobbyReady?.Invoke(lobby);
		OnLobbyMembersChanged?.Invoke(lobby);
	}

	private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
	{
		// overlay "join game" – just join lobby; OnLobbyEntered starts client
		_ = lobby.Join();
	}

	private void OnLobbyInvite(Friend friend, Lobby lobby) =>
		Debug.Log($"Invite from {friend.Name}", this);

	private void OnLobbyMemberJoined(Lobby lobby, Friend friend) 
	{ 
		OnLobbyMembersChanged?.Invoke(lobby); 
	}
	private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
	{ 
		OnLobbyMembersChanged?.Invoke(lobby); 
	}
	private void OnLobbyGameCreated(Lobby lobby, uint i, ushort s, SteamId id) { }

	// ------------------ Helpers ------------------

	private static string HashPassword(string pw)
	{
		using var sha = SHA256.Create();
		var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pw));
		var sb = new StringBuilder(bytes.Length * 2);
		foreach (var b in bytes) sb.Append(b.ToString("x2"));
		return sb.ToString();
	}
	public void LeaveGameAndReturnToMenu()
	{
		// Called by both host and client when pressing the "Leave" button

		LastNetworkErrorMessage = null; // optional, or set a friendly message

		// If we’re host or client, cleanly shut down
		Disconnect();

		// Load menu locally
		SceneManager.LoadScene(mainMenuSceneName);
	}

}
