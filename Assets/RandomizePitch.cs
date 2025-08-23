using UnityEngine;

public class RandomizePitch : MonoBehaviour
{
    public float maxPitch = 1f, minPitch = .8f;
	private void Start()
	{
		EventManager.instance.RandomizePitchOnSound(GetComponent<AudioSource>().clip, minPitch, maxPitch);
	}
}
