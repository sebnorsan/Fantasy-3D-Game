using UnityEngine;

public class RandomizePitch : MonoBehaviour
{
    public float maxPitch = 1f, minPitch = .8f;
	private void Start()
	{
		AudioManagement.instance.RandomizePitchOnSound(GetComponent<AudioSource>(), minPitch, maxPitch);
	}
}
