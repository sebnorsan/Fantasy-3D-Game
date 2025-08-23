using UnityEngine;
using TMPro;
public class ChangeFaceText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI faceText;
    public void ChangeFace(string s)
    {
        faceText.text = s;
    }
    public void ChangeFaceToCurrentWave()
    {
        int tempWave = EnemySpawnerManager.instance._currentWave+2;
        ChangeFace($"Wave {tempWave}");
    }
}
