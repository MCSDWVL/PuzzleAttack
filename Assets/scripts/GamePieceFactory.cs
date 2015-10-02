using UnityEngine;
using System.Collections;

public class GamePieceFactory : MonoBehaviour
{
	public GamePiece PiecePrefab;

	public void MutateGamePiece(ref GamePiece piece, int matchGroup)
	{
		piece.MatchGroup = matchGroup;
		GenerateGamePieceAppearance(piece);
	}

	public GamePiece CreateGamePiece(int MatchGroup = GamePiece.EMPTY_MATCH_GROUP)
	{
		var newPiece = GameObject.Instantiate(PiecePrefab) as GamePiece;
		newPiece.MatchGroup = GamePiece.EMPTY_MATCH_GROUP;
		GenerateGamePieceAppearance(newPiece);
		newPiece.MotherFactory = this;
		return newPiece;
	}

	// Act like piece appearances can be generated on the fly, idk if that's actually a good idea.
	public Color[] GamePieceColors = { Color.clear, Color.red, Color.cyan, Color.yellow, Color.green, Color.magenta, Color.blue };
	public void GenerateGamePieceAppearance(GamePiece piece)
	{
		var sprite = piece.GetComponent<SpriteRenderer>();
		if (sprite)
		{
			sprite.color = GamePieceColors[Mathf.Min(piece.MatchGroup, GamePieceColors.Length - 1)];
		}

		// TODO: add a little shapey thing
	}
}
