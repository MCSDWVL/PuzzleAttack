using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// TODO: change this to interact with an updater instead of being one.
public class PlayerInputManager : BoardUpdater 
{
	public GameBoard Board;
	public GameObject Cursor;
	private List<BoardAction> action_ = new List<BoardAction>(1);
	
	int cursorRow;
	int cursorCol;

	private BoardAction HandlePlayerInput()
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
			return CreateSwap();
		return null;
	}

	private BoardAction CreateSwap()
	{
		Debug.Log("Creating action");
		SwapAction action = new SwapAction();
		action.Row = cursorRow;
		action.Column = cursorCol;
		action.Direction = GameBoard.BoardDirection.Right;
		action.Speed1 = GlobalTuning.Instance.SwapSpeed;
		return action;
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

	public override IEnumerable<BoardAction> UpdateBoard(GameBoard board, float dt)
	{
		if (Board.GameOver)
			return Enumerable.Empty<BoardAction>();

		BoardAction action = HandlePlayerInput();
		action_.Clear();
		if (action != null)
		{
			action_.Add(action);
			Debug.Log("Adding action");
		}
		return action_.AsReadOnly();
	}

	private void Start()
	{
		UpdateCursorWorldPosition();
	}
}
