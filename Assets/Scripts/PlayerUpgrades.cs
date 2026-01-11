using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
	[SerializeField] private KeyCode openKey;

	[Space(3)]

	[SerializeField] private int cardUpgradeAmounts = 3;
	[SerializeField] private Animator anim;

	private void Update()
	{
		if (PlayerManager.instance.playerExp.IsSkillPointAvailable())
			anim.SetBool("SkillAvailable", true);
		else
		{
			anim.SetBool("SkillAvailable", false);
			return;
		}

		if (Input.GetKeyDown(openKey))
		{
			if (EventManager.instance != null && EventManager.instance.IsCameraPlayerMode())
				EventManager.instance.CameraModeMenu();

			anim.ResetTrigger("Enter");
			anim.SetTrigger("Enter");
		}
		else if (Input.GetKeyUp(openKey))
		{
			if (EventManager.instance != null && !EventManager.instance.IsCameraPlayerMode())
				EventManager.instance.CameraModePlayer();

			anim.ResetTrigger("Exit");
			anim.SetTrigger("Exit");
		}
	}

	private void ResetCards()
	{

	}
}
