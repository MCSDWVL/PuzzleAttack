using UnityEngine;

public class SwapAction : BoardAction 
{
	public GameBoard.BoardDirection Direction;
	public float Speed1 = Mathf.Infinity;
	public float Speed2 = -1f;
	public bool ForceAgainstGarbage { get; set; }
	
	public override void PerformAction(GameBoard board) {
		PerformSwap(board);
	}
	
	private void PerformSwap(GameBoard board)
	{
		Debug.Log("Performing swap");
		// If only one speed specified, use it for both piece.
		if (Speed2 < 0f)
			Speed2 = Speed1;

		// Get the pieces
		var piece1 = board.GetPieceAt(Row, Column);
		int r2, c2;
		if (!board.GetNeighborPosition(Row, Column, out r2, out c2, Direction))
		{
			Debug.LogError("Swapping off the board!");
			return;
		}
		var piece2 = board.GetPieceAt(r2, c2);

		// Don't swap pieces in motion.
		var somePieceMoving = piece1.IsMoving || piece2.IsMoving;
		var somePieceDestroying = piece1.IsCombining || piece2.IsCombining;
		if (somePieceMoving || somePieceDestroying)
		{
			Debug.Log("Bailing because moving or destroying");
			return;
		}

		var somePieceIsGarbage = piece1.IsGarbage || piece2.IsGarbage;
		if (!ForceAgainstGarbage && somePieceIsGarbage)
		{
			Debug.Log("Bailing because garbage");
			return;
		}

		// Tell each piece to begin its swap.
		var targetPos1 = piece1.transform.localPosition;
		var targetPos2 = piece2.transform.localPosition;
		piece1.SwapTo(targetPos2, Speed1);
		piece2.SwapTo(targetPos1, Speed2);
		
		// Update the boards internal idea of where each piece is, this happens 
		// instantly regardless of fall/swap animation time.
		board.SetPieceAt(Row, Column, piece2);
		board.SetPieceAt(r2, c2, piece1);
	}
}