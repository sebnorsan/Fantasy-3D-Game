using UnityEngine;
using UnityEngine.Events;
using System;

public class MonobehaviourEvent : AbstractEvent
{
	[Serializable] public class MonobehaviourRunEvent : UnityEvent { }

	[SerializeField] private MonobehaviourRunEvent onRun = new MonobehaviourRunEvent();


	public override void CallEvent()
	{
		onRun?.Invoke();
	}
}
