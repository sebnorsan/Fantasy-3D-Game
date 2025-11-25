using Unity.Netcode.Components;

public class OwnerNetworkAnimator : NetworkAnimator
{
	// This tells NGO: the owner client drives the Animator, not the server
	protected override bool OnIsServerAuthoritative()
	{
		return false;
	}
}
