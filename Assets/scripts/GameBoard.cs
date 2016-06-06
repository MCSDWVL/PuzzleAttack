using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

	public System.Random OtherRandomGenerator { get; set; }
	public System.Random BoardRandomGenerator { get; set; }
	public int RandomSeed = -1;

	public bool GameOver { get; set; }

	// Events
	public delegate void RowAddedHandler(GameBoard board);
	public event RowAddedHandler RowAdded;
	public void OnRowAdded(GameBoard board) { if (RowAdded != null) RowAdded(board); }

	public delegate void TotalComboHandler(GameBoard board, int count);
	public event TotalComboHandler TotalCombo;
	public void OnTotalCombo(GameBoard board, int count) { if (TotalCombo != null) TotalCombo(board, count); }

	private void Awake()
	{
		if (RandomSeed <= 0)
			RandomSeed = new System.Random().Next();
		
		BoardRandomGenerator = new System.Random(RandomSeed);
		OtherRandomGenerator = new System.Random(RandomSeed);

		UpdateTargetPositionForScrolling();
		InitializeEmptyBoard(Rows, Cols, PieceFactory);
		FillStartingBoard(StartingPieces);
	}

	private void Start()
	{
		timeTillNextRow = GlobalTuning.Instance.SecondsPerRowAdd;
	}

	GamePiece CreateEmptyPieceAt(int r, int c, IPieceFactory factory)
	{
		var piece = factory.CreateGamePiece();
		piece.MotherBoard = this;
		piece.transform.localPosition = LocalPositionAt(r, c);
		piece.transform.parent = transform;
		SetPieceAt(r, c, piece);
		return piece;
	}

	public void InitializeFromRemote(int seed = -1)
	{
		ClearBoard(PieceFactory);
		if(seed > 0)
			RandomSeed = seed;

		if (RandomSeed <= 0)
			RandomSeed = new System.Random().Next();

		BoardRandomGenerator = new System.Random(RandomSeed);
		OtherRandomGenerator = new System.Random(RandomSeed);

		InitializeEmptyBoard(Rows, Cols, PieceFactory);
		FillStartingBoard(StartingPieces);
	}

	public void ClearBoard(IPieceFactory factory)
	{
		foreach (var piece in pieces)
		{
			factory.MutateGamePiece(piece, GamePiece.EMPTY_MATCH_GROUP);
		}
	}

	public void InitializeEmptyBoard(int rows, int columns, IPieceFactory factory) 
	{
		if (pieces != null)
			return;
		
		pieces = new GamePiece[rows, columns];
		swapBuffer = new GamePiece[rows, columns];

		// create an empty game piece for each spot
		for (var r = 0; r < rows; ++r)
		{
			for (var c = 0; c < Cols; ++c)
			{
				var piece = CreateEmptyPieceAt(r, c, factory);
				piece.PieceComboed += OnPieceComboed;
			}
		}
	}

	public Vector3 LocalPositionAt(int row, int column)
	{
		return new Vector3(column * pieceSpacing, row * pieceSpacing, 0f);
	}

	private void FillStartingBoard(int numFilledPieces)
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
			PieceFactory.MutateGamePiece(piece, newMatchGroup);
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

	public List<GamePiece> GetNeighbors(int r, int c)
	{
		var ret = new List<GamePiece>();
		ret.Add(GetNeighbor(r, c, BoardDirection.Up));
		ret.Add(GetNeighbor(r, c, BoardDirection.Left));
		ret.Add(GetNeighbor(r, c, BoardDirection.Down));
		ret.Add(GetNeighbor(r, c, BoardDirection.Right));
		return ret;
	}

	private int piecesComboedSinceLastLateUpdate;
	private void OnPieceComboed(GamePiece piece)
	{
		++piecesComboedSinceLastLateUpdate;
		// Check for surrounding garbage and ...?
		int row = piece.Row;
		int col = piece.Col;
		foreach (var neighbor in GetNeighbors(row, col))
		{
			if (neighbor != null && neighbor.IsGarbage)
			{
				foreach (var connected in neighbor.ConnectedPieces)
				{
					var dist = Mathf.Abs(connected.Row - row) + Mathf.Abs(connected.Col - col);
					connected.BeginDegarbify(dist);
				}
			}
		}
	}

	private void LateUpdate()
	{
		if (piecesComboedSinceLastLateUpdate > 0)
		{
			OnTotalCombo(this, piecesComboedSinceLastLateUpdate);
		}
		piecesComboedSinceLastLateUpdate = 0;
	}
	
	public void LinkGarbage() {
		// Link all garbage pieces together.
		List<GamePiece> garbageGroup = new List<GamePiece>();
		List<GamePiece> unmatchedGarbage = new List<GamePiece>();

		do
		{
			unmatchedGarbage.Clear();
			unmatchedGarbage.AddRange(from GamePiece piece in pieces where piece.MatchGroup > NumPieceTypes && !piece.IsGarbage select piece);

			if (unmatchedGarbage.Count <= 0)
				break;

			garbageGroup.Clear();
			garbageGroup.AddRange(from GamePiece piece in pieces where piece.MatchGroup == unmatchedGarbage[0].MatchGroup select piece);
			if (garbageGroup == null)
				break;
			foreach (var garbagePiece in garbageGroup)
			{
				garbagePiece.IsGarbage = true;
				garbagePiece.ConnectedPieces.AddRange(garbageGroup);
			}
		} while (unmatchedGarbage.Count > 0);
	}

	public bool DoFallCheck = false;
	public bool DoCombineCheck = true;
	public bool InsertRowDebug = false;
	public bool InsertGarbageDebug = false;
	public string DebugBoardString;
	private float timeTillNextRow { get; set; }
	private static int ACTION_CAPACITY = 100;
	public BoardUpdater[] BoardUpdaters;
	public List<BoardAction> actionList_ = new List<BoardAction>(ACTION_CAPACITY);
	private void Update()
	{
		if (GameOver)
			return;
			
		actionList_.Clear();

		foreach (var updater in BoardUpdaters)
			actionList_.AddRange(updater.UpdateBoard(this, Time.deltaTime));

		foreach (var action in actionList_) 
			action.PerformAction(this);
		
		if (InsertRowDebug)
		{
			MoveUpOneRow();
			InsertRowDebug = false;
		}

		if (InsertGarbageDebug)
		{
			InsertGarbagePiece(10,1,1,1);
			InsertGarbageDebug = false;
		}

		// TODO(mvassilakos): convert board slide to updater.
		// slide up.
		this.transform.localPosition = Vector3.Lerp(StartPosition, TargetPosition, (GlobalTuning.Instance.SecondsPerRowAdd - timeTillNextRow) / GlobalTuning.Instance.SecondsPerRowAdd);

		timeTillNextRow -= Time.deltaTime;
		if (timeTillNextRow <= 0)
		{
			timeTillNextRow = GlobalTuning.Instance.SecondsPerRowAdd;
			MoveUpOneRow();
		}
	}

	private int GetRandomFactorForGarbageColumns(int number)
	{
		// Cap the max garbageGroup blocks.
		if (number > 12)
			number = 12;

		// 7 won't fit in any config, make it 6
		if (number == 7)
			number = 6;

		if (number == 11)
			number = 10;

		switch (number)
		{
			case 4:
				return new int[]{2,4}[OtherRandomGenerator.Next(0,1)];
			case 6:
				return new int[]{2,3,6}[OtherRandomGenerator.Next(0,2)];

			// Don't do more than the number of columns!  Weird and hacky from here down..
			case 8:
				return new int[] { 2, 4 }[OtherRandomGenerator.Next(0, 1)];
			case 9:
				return 3;
			case 10:
				return 5;
			case 12:
				return new int[] { 3,4,6 }[OtherRandomGenerator.Next(0, 2)];

			default:
				return number;
		}
	}

	public void InsertGarbageRandomly(int totalSize)
	{
		// What shape should the garbage be?  Pick a random factor?  For now let's just
		int garboWidth = GetRandomFactorForGarbageColumns(totalSize);
		int garboHeight = totalSize / garboWidth;

		// Insert at the top
		int rowInsert = ActualRows - garboHeight - 1; 

		// Insert at a spot where it will fit.
		int colInsert = ActualCols - garboWidth > 0 ? OtherRandomGenerator.Next(0, ActualCols - garboWidth) : 0;
		InsertGarbagePiece(rowInsert, colInsert, garboWidth, garboHeight);
	}

	int garbageCounter = 0;
	public void InsertGarbagePiece(int r, int c, int width, int height)
	{
		Debug.Log("igp: " + r + " " + c + " " + width + " " + height);
		//
		List<GamePiece> createdGarbagePieces = new List<GamePiece>();
		for (var row = r; row < r + height; ++row)
		{
			for (var col = c; col < c + width; ++col)
			{
				var piece = GetPieceAt(row, col);
				PieceFactory.MutateGamePiece(piece, NumPieceTypes + 1 + garbageCounter);
				createdGarbagePieces.Add(piece);
				piece.IsGarbage = true;
			}
		}

		foreach (var piece in createdGarbagePieces)
		{
			piece.ConnectedPieces.AddRange(createdGarbagePieces);
		}

		++garbageCounter;
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
			var piece = GetPieceAt(ActualRows - 1, c);
			if (piece.MatchGroup != GamePiece.EMPTY_MATCH_GROUP)
			{
				GameOver = true;
				return true;
			}
			piece.PieceComboed -= OnPieceComboed;
			GameObject.Destroy(piece.gameObject);
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
				var piece = CreateEmptyPieceAt(0, c, PieceFactory);
				piece.PieceComboed += OnPieceComboed;
				var newMatchGroup = BoardRandomGenerator.Next(1, NumPieceTypes);
				PieceFactory.MutateGamePiece(piece, newMatchGroup);
			}
		}
		else
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				SetPieceAt(0, c, newRow[c]);
			}
		}

		OnRowAdded(this);
		return false;
	}

	public bool IsInSteadyState()
	{
		foreach (var piece in pieces)
		{
			if (piece.IsCombining)
				return false;

			if (piece.IsDegarbifying)
				return false;

			if (piece.IsMoving)
				return false;
		}

		return true;
	}
}
