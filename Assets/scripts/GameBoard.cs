using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameBoard : MonoBehaviour 
{
	// All extant pieces by position.
	GamePiece[,] pieces;
	GamePiece[,] swapBuffer;

	// These helpers are because I consistently fuck up whether rows or colums goes first
	public int ActualRows { get { return pieces != null ? pieces.GetLength(0) : 0; } }
	public int ActualCols { get { return pieces != null ? pieces.GetLength(1) : 0; } }
	public GamePiece GetPieceAt(int row, int column) { return pieces[row, column]; }
	public void SetPieceAt(int row, int column, GamePiece piece) { pieces[row, column] = piece; piece.Row = row; piece.Col = column; }

	public int Rows = 13;
	public int Cols = 6;
	public int NumPieceTypes = 5;
	public int StartingPieces = 25;

	public GamePieceFactory PieceFactory;
	public PlayerInputManager PlayerInput;

	public float pieceSpacing = 10f;

	public System.Random BoardRandomGenerator { get; set; }
	public int RandomSeed = -1;

	public bool GameOver { get; set; }

	private void Awake()
	{
		if (RandomSeed > 0)
			BoardRandomGenerator = new System.Random(RandomSeed);
		else
			BoardRandomGenerator = new System.Random();

		UpdateTargetPositionForScrolling();
		InitializeEmptyBoard(Rows, Cols);
		FillStartingBoard(StartingPieces);
	}

	private void Start()
	{
		timeTillNextRow = GlobalTuning.Instance.SecondsPerRowAdd;
	}

	GamePiece CreateEmptyPieceAt(int r, int c)
	{
		var piece = PieceFactory.CreateGamePiece();
		piece.MotherBoard = this;
		piece.transform.localPosition = LocalPositionAt(r, c);
		piece.transform.parent = transform;
		SetPieceAt(r, c, piece);
		return piece;
	}

	void InitializeEmptyBoard(int rows, int columns) 
	{
		pieces = new GamePiece[rows, columns];
		swapBuffer = new GamePiece[rows, columns];

		// create an empty game piece for each spot
		for (var r = 0; r < rows; ++r)
		{
			for (var c = 0; c < Cols; ++c)
			{
				CreateEmptyPieceAt(r, c);
			}
		}
	}

	public Vector3 LocalPositionAt(int row, int column)
	{
		return new Vector3(column * pieceSpacing, row * pieceSpacing, 0f);
	}

	void FillStartingBoard(int numFilledPieces)
	{
		var numAddedPieces = 0;
		while (numAddedPieces < numFilledPieces)
		{
			var randR = BoardRandomGenerator.Next(0, ActualRows);
			var randC = BoardRandomGenerator.Next(0, ActualCols);
			var piece = GetPieceAt(randR, randC);
			if (!piece.IsEmpty)
				continue;

			var newMatchGroup = BoardRandomGenerator.Next(1, NumPieceTypes);
			PieceFactory.MutateGamePiece(ref piece, newMatchGroup);
			numAddedPieces++;
		}
	}

	// Manhatten directions.
	public enum BoardDirection { Up, Down, Left, Right };

	// Find the R and C of a neighbor, return false and set outputs to -1 if neighbor position is invalid.
	public bool GetNeighborPosition(int r, int c, out int neighborR, out int neighborC, BoardDirection direction)
	{
		neighborR = r;
		neighborC = c;
		switch (direction)
		{
			case BoardDirection.Down:
				neighborR -= 1;
				break;
			case BoardDirection.Up:
				neighborR += 1;
				break;
			case BoardDirection.Left:
				neighborC -= 1;
				break;
			case BoardDirection.Right:
				neighborC += 1;
				break;
		}

		if (neighborC < 0 || neighborC >= ActualCols || neighborR < 0 || neighborR >= ActualRows)
		{
			neighborR = neighborC = -1;
			return false;
		}

		return true;
	}

	// Get the piece that is the neighbor in the direction, or null if that position is out of bounds.
	public GamePiece GetNeighbor(int r, int c, BoardDirection direction)
	{
		if(!GetNeighborPosition(r, c, out r, out c, direction))
			return null;

		return GetPieceAt(r, c);
	}

	public void PerformSwap(int r, int c, BoardDirection direction, float speed1 = Mathf.Infinity, float speed2 = -1f)
	{
		// If only one speed specified, use it for both piece.
		if (speed2 < 0f)
			speed2 = speed1;

		// Get the pieces
		var piece1 = GetPieceAt(r, c);
		int r2, c2;
		if (!GetNeighborPosition(r, c, out r2, out c2, direction))
		{
			Debug.LogError("Swapping off the board!");
			return;
		}
		var piece2 = GetPieceAt(r2, c2);

		// Don't swap pieces in motion.
		if (piece1.IsMoving || piece2.IsMoving || piece1.IsCombining || piece2.IsCombining)
			return;

		// Tell each piece to begin its swap.
		var targetPos1 = piece1.transform.localPosition;
		var targetPos2 = piece2.transform.localPosition;
		piece1.SwapTo(targetPos2, speed1);
		piece2.SwapTo(targetPos1, speed2);
		
		// Update the boards internal idea of where each piece is, this happens instantly regardless of fall/swap animation time.
		SetPieceAt(r, c, piece2);
		SetPieceAt(r2, c2, piece1);
	}

	bool PieceCanFall(int r, int c)
	{
		var piece = GetPieceAt(r, c);

		if (piece.ForceFall)
			return true;

		// Empty or moving pieces can't fall.
		if (piece.IsEmpty || piece.IsMoving)
			return false;

		// Connected pieces need to be handled specially.
		if (!piece.IsConnected)
		{
			var below = GetNeighbor(r, c, BoardDirection.Down);
			return below != null && below.IsEmpty;
		}
		else
		{
			// Look at each connected piece, checking taht beneath it is a piece it's connected to, or an empty space, so that they can all fall as a group.
			foreach (var connectedPiece in piece.ConnectedPieces)
			{
				var belowConnected = GetNeighbor(connectedPiece.Row, connectedPiece.Col, BoardDirection.Down);
				if (belowConnected == null || (!belowConnected.IsEmpty && !connectedPiece.ConnectedTo(belowConnected)))
				{
					return false;
				}
			}
			return true;
		}
	}

	// Check every piece for having a "hole" beneath it, and trigger a fall if it does.
	void CheckForFalls()
	{
		// HACK: first mark every piece as not falling...
		for (var r = 0; r < ActualRows; ++r)
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				GetPieceAt(r, c).IsFalling = false;
			}
		}

		// Check each piece for fall ability.
		for (var r = 0; r < ActualRows; ++r)
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				if (PieceCanFall(r, c))
				{
					var thisPiece = GetPieceAt(r, c);
					
					// All connected pieces need to fall at once.
					if (thisPiece.IsConnected && !thisPiece.ForceFall)
					{
						foreach (var piece in thisPiece.ConnectedPieces)
						{
							piece.ForceFall = true;
						}
					}

					PerformSwap(thisPiece.Row, thisPiece.Col, BoardDirection.Down, GlobalTuning.Instance.FallSpeed, Mathf.Infinity);
					thisPiece.ForceFall = false;
				}
			}
		}
	}

	int CountMatches(int r, int c, BoardDirection direction)
	{
		var thisPiece = GetPieceAt(r, c);
		if (thisPiece.IsEmpty || thisPiece.IsMoving || thisPiece.IsFalling)
			return 0;

		var matches = 0;
		while (GetNeighborPosition(r, c, out r, out c, direction))
		{
			var matchPiece = GetPieceAt(r, c);
			if (matchPiece.IsMoving || matchPiece.IsFalling)
				break;
			if (matchPiece.MatchGroup != thisPiece.MatchGroup)
				break;
			++matches;
		}
		return matches;
	}

	public float GlobalDeathCountdown { get; set; }
	public float GlobalDeathDelay { get; set; }
	void CheckForCombos()
	{
		for (var r = 0; r < ActualRows; ++r)
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				var piece = GetPieceAt(r, c);

				if (piece.IsGarbage)
					continue;

				// TODO: we only have to check each piece in each direction once, this is overly expensive.
				// SUPERTODO: ugh this is the least efficient ever.
				var rMatches = CountMatches(r, c, BoardDirection.Right);
				var lMatches = CountMatches(r, c, BoardDirection.Left);
				var dMatches = CountMatches(r, c, BoardDirection.Down);
				var uMatches = CountMatches(r, c, BoardDirection.Up);

				var vMatches = uMatches + dMatches + 1;
				var hMatches = rMatches + lMatches + 1;

				var matches = Mathf.Max(vMatches, hMatches);
				
				piece.MatchingCount = matches;

				if (matches >= GlobalTuning.Instance.MinMatchingPieces)
				{
					// TODO: resolve the responsibility here, half things being done by board half by piece in a way that doesn't make sense.
					if (piece.IsCombining || piece.IsFalling || piece.IsMoving)
						continue;
					piece.BeginCombo();
					if (GlobalDeathCountdown <= 0f)
						GlobalDeathCountdown = GlobalTuning.Instance.CombineTime;
					else
						GlobalDeathDelay = GlobalTuning.Instance.DelayOnCombo;
				}
			}
		}
	}

	public bool DoFallCheck = false;
	public bool DoCombineCheck = true;
	public bool InsertRowDebug = false;
	public bool ResetDoLoopFlagsAfterProcessing = false;
	public bool InsertGarbageDebug = false;
	private float timeTillNextRow { get; set; }
	private void Update()
	{
		if (GameOver)
			return;

		if (DoFallCheck)
			CheckForFalls();
		if (DoCombineCheck)
			CheckForCombos();

		if (ResetDoLoopFlagsAfterProcessing)
			DoFallCheck = DoCombineCheck = false;

		if (InsertRowDebug)
		{
			MoveUpOneRow();
			InsertRowDebug = false;
		}

		if (InsertGarbageDebug)
		{
			InsertGarbagePiece(10,1,4,3);
			InsertGarbageDebug = false;
		}

		// slide up.
		this.transform.localPosition = Vector3.Lerp(StartPosition, TargetPosition, (GlobalTuning.Instance.SecondsPerRowAdd - timeTillNextRow) / GlobalTuning.Instance.SecondsPerRowAdd);

		timeTillNextRow -= Time.deltaTime;
		if (timeTillNextRow <= 0)
		{
			timeTillNextRow = GlobalTuning.Instance.SecondsPerRowAdd;
			MoveUpOneRow();
		}

		if (GlobalDeathDelay > 0)
			GlobalDeathDelay -= Time.deltaTime;
		else
			GlobalDeathCountdown -= Time.deltaTime;
	}

	private void InsertGarbagePiece(int r, int c, int width, int height)
	{
		//
		List<GamePiece> createdGarbagePieces = new List<GamePiece>();
		for (var row = r; row < r + height; ++row)
		{
			for (var col = c; col < c + width; ++col)
			{
				var piece = GetPieceAt(row, col);
				PieceFactory.MutateGamePiece(ref piece, NumPieceTypes+1);
				createdGarbagePieces.Add(piece);
				piece.IsGarbage = true;
			}
		}

		foreach (var piece in createdGarbagePieces)
		{
			piece.ConnectedPieces.AddRange(createdGarbagePieces);
		}
	}

	private void UpdateTargetPositionForScrolling()
	{
		this.transform.localPosition = StartPosition = TargetPosition;
		var newTargetPosition = this.transform.localPosition;
		newTargetPosition.y += this.pieceSpacing;
		TargetPosition = newTargetPosition;
	}

	public Vector3 TargetPosition { get; set; }
	public Vector3 StartPosition { get; set; }
	private bool MoveUpOneRow(GamePiece[] newRow = null)
	{
		GlobalTuning.Instance.OnRowAdded();
		UpdateTargetPositionForScrolling();

		PlayerInput.HandleRowInserted();

		// Check if the top row breaks (aka game over)
		for (var c = 0; c < ActualCols; ++c)
		{
			if (GetPieceAt(ActualRows - 1, c).MatchGroup != GamePiece.EMPTY_MATCH_GROUP)
			{
				GameOver = true;
				return true;
			}

			// TODO: piece pooling!  Just move these to the bottom?
			GameObject.Destroy(GetPieceAt(ActualRows - 1, c).gameObject);
		}

		// Move every piece up one (except the last row!  Can't move it up...
		for (var r = ActualRows-2; r >= 0; --r)
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				SetPieceAt(r + 1, c, GetPieceAt(r, c));
			}
		}
		
		// Insert the new row at the bottom.
		if (newRow == null)
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				var piece = CreateEmptyPieceAt(0, c);
				var newMatchGroup = BoardRandomGenerator.Next(1, NumPieceTypes);
				PieceFactory.MutateGamePiece(ref piece, newMatchGroup);
			}
		}
		else
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				SetPieceAt(0, c, newRow[c]);
			}
		}

		return false;
	}
}
