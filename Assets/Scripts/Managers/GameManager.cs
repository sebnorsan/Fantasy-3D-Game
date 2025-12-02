using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
	[SerializeField] private int currentXp = 0;
	//[SerializeField] private int currentCurrency = 0;
    //private int displayedCurrency;
	[SerializeField] private int xpToLevelUp = 10;
    [SerializeField] private Slider xpSlider;
    [SerializeField] private float xpLerpDuration = 2f;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI levelPointsText;
    //[SerializeField] private TextMeshProUGUI cashText;
	[SerializeField] private int currLevel, skillPointsAvailable;
	[SerializeField] private GameObject skillTree;
	public int xpMultiplier = 1;
	private void Awake()
	{
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;

		//displayedCurrency = currentCurrency;
		//cashText.text = $"{currentCurrency}$";

		UpdateXpGraphics();
		//UpdateCashGraphics();

		skillTree.SetActive(false);
	}
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.I))
		{
			if (!FindFirstObjectByType<PlayerController>().canMove && !skillTree.activeSelf)
				return;

			skillTree.SetActive(!skillTree.activeSelf);
			FindFirstObjectByType<PlayerController>().canMove = !FindFirstObjectByType<PlayerController>().canMove;
			FindFirstObjectByType<HandsSmooth>().enabled = !FindFirstObjectByType<HandsSmooth>().enabled;
			FindFirstObjectByType<BowScript>().enabled = !FindFirstObjectByType<BowScript>().enabled;
			Cursor.visible = !Cursor.visible;
			if (Cursor.lockState == CursorLockMode.Locked)
				Cursor.lockState = CursorLockMode.Confined;
			else
				Cursor.lockState = CursorLockMode.Locked;
		}
	}
	public void AddXp(int xp)
    {
		currentXp += xp * xpMultiplier;

		TryLevelUp();
        UpdateXpGraphics();
	}
    private void UpdateXpGraphics()
    {
        levelText.text = $"Lv. {currLevel}";
        if (skillPointsAvailable > 0)
            levelPointsText.text = $"Skill points: {skillPointsAvailable}";
        else
            levelPointsText.text = string.Empty;
        if (xpRoutine != null)
            StopCoroutine(xpRoutine);
		xpRoutine = StartCoroutine(LerpXpBar(xpLerpDuration));
	}
    Coroutine xpRoutine;
	private System.Collections.IEnumerator LerpXpBar(float duration)
	{
        float from = xpSlider.value;
        float to = (float)currentXp / xpToLevelUp;

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);

			// apply SmoothStep to ease in & out
			float smoothT = Mathf.SmoothStep(0f, 1f, t);

			xpSlider.value = Mathf.Lerp(from, to, smoothT);
			yield return null;
		}
		xpSlider.value = to;
		xpRoutine = null;
	}
	private void TryLevelUp()
    {
        if (currentXp < xpToLevelUp) return;

        currentXp -= xpToLevelUp;
        xpToLevelUp *= 2;

        currLevel++;
        GainSkillPoint(1);
    }
    public bool TrySpendSkillPoint()
    {
        if (skillPointsAvailable >= 1)
        {
            skillPointsAvailable--;
			UpdateXpGraphics();
			return true;
        }
        return false;
    }
    public void GainSkillPoint(int points)
    {
        skillPointsAvailable += points;
    }
    /// <summary>
    /// Next part here is for the skilltree
    /// </summary>
 //   public void AddCurrency(int curr)
 //   {
 //       currentCurrency += curr;
 //       UpdateCashGraphics();
 //   }
 //   Coroutine cashRoutine;
    
 //   private void UpdateCashGraphics()
 //   {
	//	if (cashRoutine != null)
	//		StopCoroutine(cashRoutine);
	//	cashRoutine = StartCoroutine(CountCurrency(.3f));
	//}
	//private System.Collections.IEnumerator CountCurrency(float duration)
	//{
 //       int from = displayedCurrency;
 //       int to = currentCurrency;

	//	int delta = to - from;
	//	if (delta == 0)
	//	{
	//		yield break;
	//	}

	//	int step = delta > 0 ? 1 : -1;
	//	int steps = Mathf.Abs(delta);
	//	float interval = duration / steps;
	//	int current = from;

	//	for (int i = 0; i < steps; i++)
	//	{
	//		current += step;
	//		cashText.text = $"{current}$";
	//		yield return new WaitForSeconds(interval);
	//	}

	//	// ensure exact final value
	//	cashText.text = $"{to}$";
	//	displayedCurrency = to;
	//	cashRoutine = null;
	//}
	//private bool TryBuySkill(int price)
 //   {
 //       if (currentCurrency >= price)
 //       {
	//		currentCurrency -= price;
	//		return true;
	//	}
	//	return false;
 //   }
 //   public void PurchaseSkill(int price)
 //   {
 //       if (TryBuySkill(price))
 //       {

 //       }
 //   }
}
