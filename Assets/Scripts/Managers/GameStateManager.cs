using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
	[SerializeField] private KeyCode changeStateKey;

	[SerializeField] private static GameStateType currentGameStateType;

	public static Action gameStateChanged;

	public static GameStateType GetCurrentGameState() => currentGameStateType;
	public static void SetCurrentGameState(GameStateType state)
	{
		currentGameStateType = state;
		gameStateChanged?.Invoke();
	}

	private void Start() => currentGameStateType = GameStateType.Morning;

	private void Update()
	{
		if (Input.GetKeyDown(changeStateKey))
		{
			if (GetCurrentGameState() == GameStateType.Morning)
				SetCurrentGameState(GameStateType.Night);
			else
				SetCurrentGameState(GameStateType.Morning);
		}
	}
}

public enum GameStateType
{
	Morning,
	Night
}