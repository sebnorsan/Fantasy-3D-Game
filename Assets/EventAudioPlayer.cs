using UnityEngine;

public class EventAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioToPlay[] audios;
    public void PlayAudio(int i)
    {
        EventManager.instance.PlayThisSound(audios[i]);
    }
}
