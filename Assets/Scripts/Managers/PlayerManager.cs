using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode;

public class PlayerManager : MonoBehaviour
{
	public static PlayerManager instance;
	public static PlayerReferences m_pRef;

	public PlayerExperience playerExp;
	public PlayerUpgrades playerUpg;

	private void Awake()
	{
		if (instance != null) Destroy(gameObject);
		else instance = this;
	}
	private IEnumerator Start()
	{
		// Wait until NGO has spawned the local player object
		while (NetworkManager.Singleton == null ||
			   NetworkManager.Singleton.LocalClient == null ||
			   NetworkManager.Singleton.LocalClient.PlayerObject == null)
		{
			yield return null;
		}

		m_pRef = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerReferences>();

		if (m_pRef == null)
			Debug.LogError("Local PlayerObject has no PlayerReferences component!");
	}
}