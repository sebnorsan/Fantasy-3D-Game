using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcherEvent : AbstractEvent
{
    [Space(25)]

	[SerializeField] private string sceneName;
    [SerializeField] private Skip whenToSkip = Skip.None;
    [SerializeField] private Color screenColor = Color.white;
    [SerializeField] private float timeToTransition = 1f;

    public override void CallEvent()
    {
        CallSceneChange();
	}

    public void CallSceneChange()
    {
        SceneManagerTransitions.whatToSkip = whenToSkip;
        SceneManagerTransitions.LoadSceneWithTransition(screenColor, timeToTransition, sceneName);
    }

}
