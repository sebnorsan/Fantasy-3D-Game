using UnityEngine;

public class WaveController : MonoBehaviour
{
    public EnemySpawnerManager spawnerManager;

    public void StartWaveAnimation()
    {
        GetComponent<Animator>().SetTrigger("NextWave");
    }
    public void StartNextWave()
    {
        spawnerManager._currentWave++;

        if (spawnerManager.ReachedFinalWave())
        {
			Debug.Log("LAST WAAAAVE");
            GetComponent<Animator>().SetTrigger("LastWave");
		}

        spawnerManager.StartWave(spawnerManager._currentWave);
    }
}
