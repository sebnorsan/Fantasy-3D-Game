using UnityEngine;

public class EventMuter : MonoBehaviour
{
	MusicManager mm;
	private void Awake()
	{
		mm = FindFirstObjectByType<MusicManager>();
	}
	public void MuteMusic()
	{
		mm.MuteAllFade();
	}
	public void UnmuteMusic()
	{
		mm.UnmuteAllFade();
	}
}
