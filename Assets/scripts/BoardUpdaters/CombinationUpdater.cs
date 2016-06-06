using UnityEngine;
using System.Collections.Generic;

public class CombinationUpdater : BoardUpdater 
{
	public GlobalDeathTimeUpdater TimeUpdater;
	private static int CAPACITY = 100;
	private List<BoardAction> boardActions_ = new List<BoardAction>(CAPACITY);

	public override IEnumerable<BoardAction> UpdateBoard(
		GameBoard board, float dt) 
	{
		boardActions_.Clear();
		CheckForCombos(board, boardActions_);		
		return boardActions_.AsReadOnly();
	}
	
	private void CheckForCombos(GameBoard board, List<BoardAction> actions)
	{
		for (var r = 0; r < board.ActualRows; ++r)
		{
			for (var c = 0; c < board.ActualCols; ++c)
			{
				var piece = board.GetPieceAt(r, c);

				if (piece.IsGarbage)
					continue;

				// TODO: we only have to check each piece in each direction once, this is overly expensive.
				// SUPERTODO: ugh this is the least efficient ever.
				var rMatches = CountMatches(board, r, c, GameBoard.BoardDirection.Right);
				var lMatches = CountMatches(board, r, c, GameBoard.BoardDirection.Left);
				var dMatches = CountMatches(board, r, c, GameBoard.BoardDirection.Down);
				var uMatches = CountMatches(board, r, c, GameBoard.BoardDirection.Up);

				var vMatches = uMatches + dMatches + 1;
				var hMatches = rMatches + lMatches + 1;

				var matches = Mathf.Max(vMatches, hMatches);
				
				piece.MatchingCount = matches;

				if (matches >= GlobalTuning.Instance.MinMatchingPieces)
				{
					// TODO: resolve the responsibility here, half things being done by board half by piece in a way that doesn't make sense.
					if (piece.IsCombining || piece.IsMoving)
						continue;
					actions.Add(new BeginCombinationAction(r, c, TimeUpdater.GlobalDeathCountdown));
				}
			}
		}
	}
	
	private int CountMatches(GameBoard board, int r, int c, GameBoard.BoardDirection direction)
	{
		var thisPiece = board.GetPieceAt(r, c);
		if (thisPiece.IsEmpty || thisPiece.IsMoving)
			return 0;

		var matches = 0;
		while (board.GetNeighborPosition(r, c, out r, out c, direction))
		{
			var matchPiece = board.GetPieceAt(r, c);
			if (matchPiece.IsMoving)
				break;
			if (matchPiece.MatchGroup != thisPiece.MatchGroup)
				break;
			++matches;
		}
		return matches;
	}
}