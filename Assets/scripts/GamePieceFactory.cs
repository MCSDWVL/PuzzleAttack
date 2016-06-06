using UnityEngine;
using System.Collections;

public class GamePieceFactory : MonoBehaviour, IPieceFactory
{
	public GamePiece PiecePrefab;

	public void MutateGamePiece(GamePiece piece, int matchGroup)
	{
		piece.ClearState();
		piece.MatchGroup = matchGroup;
		GenerateGamePieceAppearance(piece);
	}

	public GamePiece CreateGamePiece(int MatchGroup = GamePiece.EMPTY_MATCH_GROUP)
	{
		var newPiece = GameObject.Instantiate(PiecePrefab) as GamePiece;
		newPiece.MatchGroup = MatchGroup;
		GenerateGamePieceAppearance(newPiece);
		newPiece.MotherFactory = this;
		return newPiece;
	}

	// Act like piece appearances can be generated on the fly, idk if that's actually a good idea.
	public Color[] GamePieceColors = { Color.clear, Color.red, Color.cyan, Color.yellow, Color.green, Color.magenta, Color.blue };
	public void GenerateGamePieceAppearance(GamePiece piece)
	{
		if (!piece) {
			Debug.LogError("No piece prefab in factory!");
			return;
		}
		
		var sprite = piece.GetComponent<SpriteRenderer>();
		if (sprite)
		{
			sprite.color = GamePieceColors[Mathf.Min(piece.MatchGroup, GamePieceColors.Length - 1)];
		}

		// TODO: add a little shapey thing
	}
}

public interface IPieceFactory
{
	void MutateGamePiece(GamePiece piece, int matchGroup);
	GamePiece CreateGamePiece(int MatchGroup = GamePiece.EMPTY_MATCH_GROUP);
	void GenerateGamePieceAppearance(GamePiece piece);
}

