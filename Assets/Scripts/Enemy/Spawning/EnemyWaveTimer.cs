using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System;

public class EnemyWaveTimer : NetworkBehaviour
{
	[SerializeField] private TMPro.TextMeshProUGUI enemyDownTimeTimerText;
	public static Action downTimeTimerFinished;

	public void StartDownTimeTimer(int totalSeconds) => StartCoroutine(TimerCountdown(totalSeconds));
	public override void OnNetworkSpawn()
	{
		if (NetworkManager.Singleton.IsServer)
			ClearTimerClientRpc();
	}
	private IEnumerator TimerCountdown(int totalSeconds)
	{
		int remaining = totalSeconds;
		while (remaining >= 0)
		{
			UpdateTimerClientRpc(remaining);
			yield return new WaitForSeconds(1f);
			remaining--;
		}

		ClearTimerClientRpc();
		downTimeTimerFinished.Invoke();

		// Countdown finished, tell everyone to play wave intro animation
		//StartWaveAnimation();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void UpdateTimerClientRpc(int remainingSeconds)
	{
		if (enemyDownTimeTimerText == null) return;

		int minutes = remainingSeconds / 60;
		int seconds = remainingSeconds % 60;
		enemyDownTimeTimerText.text = $"{minutes}:{seconds:00}";
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void ClearTimerClientRpc()
	{
		if (enemyDownTimeTimerText != null)
			enemyDownTimeTimerText.text = string.Empty;
	}
}
