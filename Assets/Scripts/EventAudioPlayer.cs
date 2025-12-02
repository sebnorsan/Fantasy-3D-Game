using UnityEngine;

public class EventAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioToPlay[] audios;
    public void PlayAudio(int i)
    {
        AudioManagement.instance.PlayThisSound(audios[i]);
    }
    public void StopAudio(int i)
    {
		AudioManagement.instance.StopThisSound(audios[i].audioToPlay);
    }
}
