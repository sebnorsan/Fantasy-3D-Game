using UnityEngine;

public class CrystalScript : MonoBehaviour
{
    [SerializeField] private GameObject crystalShield;
	private void Start()
	{
		crystalShield.SetActive(true);
	}
}
