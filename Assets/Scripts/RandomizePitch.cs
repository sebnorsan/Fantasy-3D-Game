using UnityEngine;

public class RandomizePitch : MonoBehaviour
{
    public float maxPitch = 1f, minPitch = .8f;
	private void Start()
	{
		foreach (var source in GetComponents<AudioSource>())
			AudioManagement.instance.RandomizePitchOnSound(source, minPitch, maxPitch);
	}
}
