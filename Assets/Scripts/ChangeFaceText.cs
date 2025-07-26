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
        int tempWave = 0;
        ChangeFace($"Wave {tempWave}");
    }
}
