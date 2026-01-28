using UnityEngine;

public class GameStateManager : MonoBehaviour
{
	[SerializeField] private static GameStateType currentGameStateType;

	public static GameStateType GetCurrentGameState() => currentGameStateType;
	public static void SetCurrentGameState(GameStateType state) => currentGameStateType = state;

	private void Start() => currentGameStateType = GameStateType.Morning;
}

public enum GameStateType
{
	Morning,
	Night
}