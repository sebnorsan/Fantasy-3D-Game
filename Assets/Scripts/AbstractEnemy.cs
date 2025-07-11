using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemy : MonoBehaviour
{
	private NavMeshAgent agent;
	private Transform crystalPos;
	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		crystalPos = GameObject.FindGameObjectWithTag("Crystal").transform;
		agent.SetDestination(crystalPos.position);
		InitializeEnemy();
	}
	private void Update()
	{

	}
	public virtual void InitializeEnemy()
	{

	}
}
