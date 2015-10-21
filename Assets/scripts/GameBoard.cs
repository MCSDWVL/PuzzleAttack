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

	public void InitializeFromRemote(int seed = -1)
	{
		ClearBoard();
		if(seed > 0)
			RandomSeed = seed;

		if (RandomSeed <= 0)
			RandomSeed = new System.Random().Next();

		BoardRandomGenerator = new System.Random(RandomSeed);
		OtherRandomGenerator = new System.Random(RandomSeed);

		InitializeEmptyBoard(Rows, Cols);
		FillStartingBoard(StartingPieces);
	}

	public void ClearBoard()
	{
		foreach (var piece in pieces)
		{
			PieceFactory.MutateGamePiece(piece, GamePiece.EMPTY_MATCH_GROUP);
		}
	}

	public string Serialize()
	{
		var ret = "";
		SerializeBoard(out ret);
		return ret;
	}

	public void SerializeBoard(out string ret)
	{
		int[][] arr;
		SerializeBoard(out arr);
		ret = "";

		for (var row = 0; row < arr.Length; ++row)
		{
			ret += string.Join(SerializationHelper.COL_SEPARATOR+"", arr[row].Select(x => x.ToString()).ToArray());
			if (row != arr.Length - 1)
				ret += SerializationHelper.ROW_SEPARATOR;
		}
	}

	public void SerializeBoard(out int[][] ret)
	{
		ret = new int[ActualRows][];
		for (var row = 0; row < ActualRows; ++row)
			ret[row] = new int[ActualCols];
		foreach (var piece in pieces)
			ret[piece.Row][piece.Col] = piece.MatchGroup;
	}

	public void DeserializeBoard(string matchGroupsString)
	{
		if (pieces == null)
			InitializeEmptyBoard(Rows, Cols);
		else
			ClearBoard();

		var matchGroups = new int[ActualRows,ActualCols];
		var rowStrings = matchGroupsString.Split(SerializationHelper.ROW_SEPARATOR);
		for (var row = 0; row < ActualRows; ++row)
		{
			var colStrings = rowStrings[row].Split(SerializationHelper.COL_SEPARATOR);
			var colVals = colStrings.Select(x => int.Parse(x)).ToArray();
			for (var col = 0; col < ActualCols; ++col)
			{
				PieceFactory.MutateGamePiece(GetPieceAt(row, col), colVals[col]);
			}
		}

		// Link all garbage pieces together.
		List<GamePiece> garbageGroup = new List<GamePiece>();
		List<GamePiece> unmatchedGarbage = new List<GamePiece>();

		do
		{
			//2054772776x1c2c2c4c4c4r4c1c4c4c3c2r1c10c4c3c4c1r4c10c2c2c2c4r1c8c2c1c4c2r1c10c3c1c2c4r2c10c3c0c1c3r4c10c0c0c4c4r4c10c0c0c1c3r0c0c0c0c1c2r0c0c0c0c4c2r0c0c0c0c0c0r0c0c0c0c0c0s3g0g0g3g3s1pss0p1c2c2c4c4c4r4c1c4c4c3c2r1c0c4c3c4c1r4c0c2c2c2c4r1c0c2c1c4c2r1c0c3c1c2c4r2c0c3c0c1c3r4c0c0c0c4c4r4c0c0c0c1c3r0c0c0c0c1c2r0c0c0c0c4c2r0c0c0c0c0c0r0c0c0c0c0c0s3g0g0g3g3s1pss0
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

	void InitializeEmptyBoard(int rows, int columns) 
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
				var piece = CreateEmptyPieceAt(r, c);
				piece.PieceComboed += OnPieceComboed;
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

	public void PerformSwap(int r, int c, BoardDirection direction, float speed1 = Mathf.Infinity, float speed2 = -1f, bool forceAgainstGarbage = false)
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
		{
			return;
		}

		if (!forceAgainstGarbage && (piece1.IsGarbage || piece2.IsGarbage))
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

	FallState PieceCanFall(int r, int c)
	{
		var piece = GetPieceAt(r, c);

		// Empty or moving pieces can't fall.
		if (piece.IsEmpty || piece.IsMoving)
			return FallState.AlreadyFalling;

		// Connected pieces need to be handled specially.
		if (!piece.IsConnected)
		{
			var below = GetNeighbor(r, c, BoardDirection.Down);
			return below != null && below.IsEmpty && !below.IsMoving ? FallState.ShouldFall : FallState.ShouldNotFall;
		}
		else
		{
			// Look at each connected piece, checking taht beneath it is a piece it's connected to, or an empty space, so that they can all fall as a group.
			foreach (var connectedPiece in piece.ConnectedPieces)
			{
				if(connectedPiece.IsMoving)
					return FallState.FallImpossibleFromOtherConstraint;
				var belowConnected = GetNeighbor(connectedPiece.Row, connectedPiece.Col, BoardDirection.Down);

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

	// Check every piece for having a "hole" beneath it, and trigger a fall if it does.
	enum FallState { Unevaluated, ShouldFall, ShouldNotFall, FallImpossibleFromOtherConstraint, AlreadyFalling };
	void CheckForFalls()
	{
		// Initialize fallstate array
		FallState[,] fallstate = new FallState[ActualRows, ActualCols];
		for (var r = 0; r < ActualRows; ++r)
			for (var c = 0; c < ActualCols; ++c)
				fallstate[r, c] = FallState.Unevaluated;

		// Update each positions fallstate
		for (var r = 0; r < ActualRows; ++r)
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				var piece = GetPieceAt(r, c);
				var canFall = PieceCanFall(r, c);

				fallstate[r, c] = PieceCanFall(r, c);
			}
		}

		// Now that all fallstates have been calculated perform the actual movements.
		// NOTE it is important that this happens in order from bottom to top!
		for (var r = 0; r < ActualRows; ++r)
		{
			for (var c = 0; c < ActualCols; ++c)
			{
				if (fallstate[r,c] == FallState.ShouldFall)
				{
					CauseFall(r, c);
				}
			}
		}
	}

	private void CauseFall(int r, int c)
	{
		var thisPiece = GetPieceAt(r, c);
		PerformSwap(thisPiece.Row, thisPiece.Col, BoardDirection.Down, GlobalTuning.Instance.FallSpeed, Mathf.Infinity, true);
	}

	int CountMatches(int r, int c, BoardDirection direction)
	{
		var thisPiece = GetPieceAt(r, c);
		if (thisPiece.IsEmpty || thisPiece.IsMoving)
			return 0;

		var matches = 0;
		while (GetNeighborPosition(r, c, out r, out c, direction))
		{
			var matchPiece = GetPieceAt(r, c);
			if (matchPiece.IsMoving)
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
					if (piece.IsCombining || piece.IsMoving)
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
	public bool InsertGarbageDebug = false;
	public string DebugBoardString;
	public bool DebugSerializeBoardString;
	public bool DebugDeserializeBoard;
	private float timeTillNextRow { get; set; }
	private void Update()
	{
		if (GameOver)
			return;

		if (DoFallCheck)
			CheckForFalls();
		if (DoCombineCheck)
			CheckForCombos();

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

		if (DebugSerializeBoardString)
		{
			SerializeBoard(out DebugBoardString);
			DebugSerializeBoardString = false;
		}

		if (DebugDeserializeBoard)
		{
			DeserializeBoard(DebugBoardString);
			DebugDeserializeBoard = false;
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
				var piece = CreateEmptyPieceAt(0, c);
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
