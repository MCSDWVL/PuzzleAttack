using UnityEngine;
using System.Collections.Generic;

public class FallUpdater : BoardUpdater 
{
	private static int CAPACITY = 100;
	private List<BoardAction> boardActions_ = new List<BoardAction>(CAPACITY);

	public override IEnumerable<BoardAction> UpdateBoard(
		GameBoard board, float dt) 
	{
		FallState[,] fallstate = CheckForFalls(board);
		boardActions_.Clear();
		
		for (var r = 0; r < board.ActualRows; ++r)
		{
			for (var c = 0; c < board.ActualCols; ++c)
			{
				if (fallstate[r,c] == FallState.ShouldFall) {
					SwapAction fall = new SwapAction();
					fall.Row = r;
					fall.Column = c;
					fall.Direction = GameBoard.BoardDirection.Down;
					fall.Speed1 = GlobalTuning.Instance.FallSpeed;
					fall.Speed2 = Mathf.Infinity;
					boardActions_.Add(fall);
				}
			}
		}
		
		return boardActions_.AsReadOnly();
	}
	
	// The possible "falling" states a piece can be in.
	enum FallState { 
		// Hasn't been checked yet.
		Unevaluated,
		// Ought to fall.
		ShouldFall, 
		// Ought not to fall.
		ShouldNotFall, 
		// Sometimes a piece should fall (hole underneath) but is locked.
		FallImpossibleFromOtherConstraint, 
		// Has already begun falling.
		AlreadyFalling 
	};
	
	// Check every piece for having a "hole" beneath it, and trigger a fall if it does.
	private FallState[,] CheckForFalls(GameBoard board)
	{
		// Initialize fallstate array
		int rows = board.ActualRows;
		int cols = board.ActualCols;
		FallState[,] fallstate = new FallState[rows, cols];
		for (var r = 0; r < rows; ++r)
			for (var c = 0; c < cols; ++c)
				fallstate[r, c] = FallState.Unevaluated;

		// Update each positions fallstate
		for (var r = 0; r < rows; ++r)
		{
			for (var c = 0; c < cols; ++c)
			{
				var piece = board.GetPieceAt(r, c);
				var canFall = PieceCanFall(r, c, board);

				fallstate[r, c] = PieceCanFall(r, c, board);
			}
		}
		
		return fallstate;
	}
		
	private FallState PieceCanFall(int r, int c, GameBoard board)
	{
		var piece = board.GetPieceAt(r, c);

		// Empty or moving pieces can't fall.
		if (piece.IsEmpty || piece.IsMoving)
			return FallState.AlreadyFalling;

		// Connected pieces need to be handled specially.
		if (!piece.IsConnected)
		{
			var below = board.GetNeighbor(r, c, GameBoard.BoardDirection.Down);
			return below != null && below.IsEmpty && !below.IsMoving ? FallState.ShouldFall : FallState.ShouldNotFall;
		}
		else
		{
			// Look at each connected piece, checking taht beneath it is a piece it's connected to, or an empty space, so that they can all fall as a group.
			foreach (var connectedPiece in piece.ConnectedPieces)
			{
				if(connectedPiece.IsMoving)
					return FallState.FallImpossibleFromOtherConstraint;
				var belowConnected = board.GetNeighbor(connectedPiece.Row, connectedPiece.Col, GameBoard.BoardDirection.Down);

				// If the piece below is connected to us, we defer to the rest of the group to decide if we fall.
				if (connectedPiece.ConnectedTo(belowConnected))
					continue;

				// If a connected piece is touching the "ground" we can't fall.
				if (belowConnected == null)
					return FallState.FallImpossibleFromOtherConstraint;

				// If a connected piece has a non-empty piece below it (and we already know it's not our brother) we can't fall.
				if (!belowConnected.IsEmpty)
					return FallState.FallImpossibleFromOtherConstraint;

				// If the piece below us is moving and isn't connected to us we can't fall (wait for it to finish).
				if (belowConnected.IsMoving)
					return FallState.FallImpossibleFromOtherConstraint;
			}

			// Guess we can fall...
			return FallState.ShouldFall;
		}
	}
}