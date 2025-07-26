using UnityEngine;
using EZCameraShake;
public class EventShaker : MonoBehaviour
{
    [System.Serializable]
    public class ShakeVariables
    {
        public float magnitude, roughness, fadeIn, fadeOut;
    }
    [SerializeField] private ShakeVariables[] shakers;
    public void ShakeOnce(int i)
    {
        var currShaker = shakers[i];

        CameraShaker.Instance.ShakeOnce(
            currShaker.magnitude,
            currShaker.roughness,
            currShaker.fadeIn,
            currShaker.fadeOut);
    }
}
