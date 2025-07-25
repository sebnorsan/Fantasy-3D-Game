using UnityEngine;

public class Blink : MonoBehaviour
{
    private Animator anim;
	private void Start()
	{
		anim = GetComponent<Animator>();
		BlinkAction();
	}
	private void BlinkAction()
    {
		anim.SetTrigger("Blink");
        Invoke(nameof(BlinkAction), Random.Range(0, 12f));
    }
}
