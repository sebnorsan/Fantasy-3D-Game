using UnityEngine;

public class Blink : MonoBehaviour
{
    [SerializeField] private Animator anim;
	private void Start()
	{
		BlinkAction();
	}
	private void BlinkAction()
    {
		anim.SetTrigger("Blink");
        Invoke(nameof(BlinkAction), Random.Range(0, 12f));
    }
}
