using TMPro;
using Unity.Netcode;
using UnityEngine;
using Steamworks;
using Unity.Hierarchy;
using UnityEngine.Rendering;
using System.Collections;

public class PlayerGraphicVisuals : NetworkBehaviour
{
	[SerializeField] private PlayerReferences pRef;
	private Quaternion baseRot;

	[SerializeField] private GameObject gfx;

	[SerializeField] private bool ignoreMultiplayerGraphics = false;

	[Space(15)]

	[SerializeField] private Renderer[] multiplayerGfx;
	[SerializeField] private Renderer[] singlePlayerGfx;

	[SerializeField] private GameObject playerCanvas;

	[Space(15)]

	[SerializeField] private TextMeshProUGUI textUsername;

	[SerializeField] private float maxForwardTilt = 10f;
	[SerializeField] private float maxSideTilt = 8f;
	[SerializeField] private float tiltSpeed = 8f;

	// store the name on the server so it can be re-sent
	private string cachedName;

	[Header("Particle FX")]
	[SerializeField] private ParticleSystem haloPfx;
	[SerializeField] private ParticleSystem stripePfx;
	[SerializeField] private ParticleSystem runPfx;

	[Header("PvP")]
	[SerializeField] private ParticleSystem onFirePfx;

	public override void OnNetworkSpawn()
	{
		baseRot = transform.localRotation;

		if (IsOwner)
		{
			if (playerCanvas)
				playerCanvas.SetActive(false);

			if (!ignoreMultiplayerGraphics)
				if (multiplayerGfx != null && multiplayerGfx.Length > 0)
					foreach (var go in multiplayerGfx)
						go.enabled = false;

			// send our Steam name once when we spawn
			SetNameServerRpc(SteamClient.Name);
		}
		else
		{
			if (singlePlayerGfx != null && singlePlayerGfx.Length > 0)
				foreach (var go in singlePlayerGfx)
					go.enabled = false;

			// I’m a copy of SOMEONE ELSE’s player – ask server what their name is
			RequestNameServerRpc();
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SetNameServerRpc(string name)
	{
		cachedName = name;
		SetNameClientRpc(name);
	}

	// called by late joiners to get the name again
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void RequestNameServerRpc()
	{
		if (!string.IsNullOrEmpty(cachedName))
			SetNameClientRpc(cachedName);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetNameClientRpc(string name)
	{
		if (textUsername != null)
			textUsername.text = name;
	}

	private void Update()
	{
		if (pRef.playerController == null || gfx == null) return;

		float targetX = -pRef.playerController.inputVertical * maxForwardTilt;
		float targetZ = -pRef.playerController.inputHorizontal * maxSideTilt;

		Quaternion targetRot = baseRot * Quaternion.Euler(targetX, 0f, targetZ);
		gfx.transform.localRotation = Quaternion.Lerp(
			gfx.transform.localRotation,
			targetRot,
			Time.deltaTime * tiltSpeed
		);
	}

	public void PlayParticle(PfxToPlay pfx, bool play = true, float delay = 0f)
	{
		StartCoroutine(PlayParticleIE(pfx, play, delay));
	}
	private IEnumerator PlayParticleIE(PfxToPlay pfx, bool play = true, float delay = 0f)
	{
		yield return new WaitForSeconds(delay);

		PlayParticleFunctionality(pfx, play);
		PlayParticleServerRpc(pfx, play);
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void PlayParticleServerRpc(PfxToPlay pfx, bool play)
	{
		PlayParticleClientRpc(pfx, play);
	}
	[Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
	private void PlayParticleClientRpc(PfxToPlay pfx, bool play)
	{
		PlayParticleFunctionality(pfx, play);
	}
	private void PlayParticleFunctionality(PfxToPlay pfx, bool play)
	{
		ParticleSystem pfxToPlay = null;

		switch (pfx)
		{
			case PfxToPlay.Halo:
				pfxToPlay = haloPfx;
				break;
			case PfxToPlay.Stripe:
				pfxToPlay = stripePfx;
				break;
			case PfxToPlay.Run:
				pfxToPlay = runPfx;
				break;
			case PfxToPlay.Fire:                 // <-- ADD THIS
				pfxToPlay = onFirePfx;
				break;
		}

		if (pfxToPlay == null) return;

		if (play)
		{
			if (!pfxToPlay.isPlaying)
				pfxToPlay.Play();
		}
		else
		{
			if (pfxToPlay.isPlaying)
				pfxToPlay.Stop();
		}
	}


}
public enum PfxToPlay
{
	Halo,
	Stripe,
	Run,
	Fire
}