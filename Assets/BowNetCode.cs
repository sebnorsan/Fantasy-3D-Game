using System.Globalization;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class BowNetCode : NetworkBehaviour
{
	public void SpawnArrowRequest(
		GameObject arrowPrefab,
		Vector3 pos,
		Quaternion rot,
		Vector3 shootDir,
		int damage,
	int[] effects,
		float speed,
		bool cutsTrees,
		int chain,
		float size
	)
	{
		if (!IsOwner) return;
		SpawnArrowServerRpc(pos, rot, shootDir, damage, effects, speed, cutsTrees, chain, size);
	}

	[ServerRpc]
	private void SpawnArrowServerRpc(
		Vector3 pos,
		Quaternion rot,
		Vector3 shootDir,
		int damage,
		int[] effects,
		float speed,
		bool cutsTrees,
		int chain,
		float size,
		ServerRpcParams rpcParams = default
	)
	{
		// You should reference the prefab from a field in real code,
		// but passing the GameObject is ok if you keep it identical for all builds.
		// Safer: store arrow prefab on BowNetcode instead.
		Debug.Log("ServerRpc running. IsServer=" + NetworkManager.Singleton.IsServer);

		var arrowObj = Instantiate(GetArrowPrefab(), pos, rot);
		var arrow = arrowObj.GetComponent<Arrow>();
		var netObj = arrowObj.GetComponent<NetworkObject>();
		netObj.Spawn(true);

		Debug.Log("About to ServerInitialize. IsServerOnArrow=" + arrow.IsServer);

		arrow.ServerInitialize(
			damage,
			effects.Select(i => (ArrowEffect)i).ToArray(),
			speed,
			cutsTrees,
			chain,
			size,
			shootDir,
			transform.position,
			rpcParams.Receive.SenderClientId
		);

		Debug.Log("After ServerInitialize. velocity=" + /* expose a debug getter or log from inside init */"");
	}

	// Set this in inspector if you want (recommended).
	[SerializeField] private GameObject arrowPrefab;
	private GameObject GetArrowPrefab() => arrowPrefab;
}