using UnityEngine;
using System.Collections;

public class PlayerInputManager : MonoBehaviour 
{
	public GameBoard Board;
	public GameObject Cursor;
	
	int cursorRow;
	int cursorCol;

	private void HandlePlayerInput()
	{
		if (Input.GetButtonDown("up"))
			HandleMovement(GameBoard.BoardDirection.Up);
		else if (Input.GetButtonDown("left"))
			HandleMovement(GameBoard.BoardDirection.Left);
		else if (Input.GetButtonDown("right"))
			HandleMovement(GameBoard.BoardDirection.Right);
		else if (Input.GetButtonDown("down"))
			HandleMovement(GameBoard.BoardDirection.Down);
		else if (Input.GetButtonDown("swap"))
			HandleSwap();
	}

	private void HandleSwap()
	{
		Board.PerformSwap(cursorRow, cursorCol, GameBoard.BoardDirection.Right, GlobalTuning.Instance.SwapSpeed);
	}

	private void HandleMovement(GameBoard.BoardDirection direction)
	{
		int desiredR, desiredC;
		if (Board.GetNeighborPosition(cursorRow, cursorCol, out desiredR, out desiredC, direction) && desiredC < Board.ActualCols - 1)
		{
			cursorRow = desiredR;
			cursorCol = desiredC;
			UpdateCursorWorldPosition();
		}
	}

	// TODO: this needs to be on an event!
	private float yOffset = 0;
	public void HandleRowInserted()
	{
		// We are now actually highlighting one row higher tahn we think we are
		cursorRow += 1;

		// But our position is tied to the board sliding up... we need to adjust our GLOBAL world position and LOCAL world position to remain in the exact same spot while being a row higher.
		yOffset += Board.pieceSpacing;
		
		//var newLocal = Cursor.transform.localPosition;
		//newLocal.y += Board.pieceSpacing;
		//Cursor.transform.localPosition = newLocal;
	}

	private void UpdateCursorWorldPosition()
	{
		var newPosition = Board.LocalPositionAt(cursorRow, cursorCol);
		newPosition.x += Board.pieceSpacing / 2.0f;
		newPosition.y -= yOffset;
		Cursor.transform.localPosition = newPosition;
	}

	private void Update()
	{
		if (Board.GameOver)
			return;

		HandlePlayerInput();
	}

	private void Start()
	{
		UpdateCursorWorldPosition();
	}
}
