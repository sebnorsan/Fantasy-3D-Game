using UnityEngine;

public class InstantiateScriptIfOwner : MonoBehaviour
{
	[SerializeField] private MonoBehaviour scriptToEnable;

	private void Start()
	{
		var player = GetComponentInParent<PlayerController>();
		if (player == null || !player.IsOwner) return;

		if (scriptToEnable != null)
			scriptToEnable.enabled = true;
	}
}
