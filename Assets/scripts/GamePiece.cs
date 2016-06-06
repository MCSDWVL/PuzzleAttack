using UnityEngine;
using System.Collections.Generic;

public class GamePiece : MonoBehaviour
{
	public const int EMPTY_MATCH_GROUP = 0;
	public int MatchGroup = EMPTY_MATCH_GROUP;
	public bool IsEmpty { get { return MatchGroup == EMPTY_MATCH_GROUP; } }

	public bool IsCombining { get; set; }
	public bool IsGarbage { get; set; }
	public bool IsMoving { get; set; }
	public bool IsConnected { get { return ConnectedPieces != null && ConnectedPieces.Count > 0; } }
	public bool IsDegarbifying { get { return degarbifyTime > 0 && IsGarbage; } }

	public Vector3 TargetPosition { get; set; }
	public float MoveSpeed { get; set; }

	public GamePieceFactory MotherFactory { get; set; }
	public GameBoard MotherBoard { get; set; }

	public int MatchingCount { get; set; }

	private float deathCountdown;
	private Color startingColor;

	private SpriteRenderer sprite;

	public List<GamePiece> ConnectedPieces { get; private set; }

	public int Row { get; set; }
	public int Col { get; set; }

	private float initialDegarbifyTime;
	private float degarbifyTime;

	public delegate void PieceComboedHandler(GamePiece piece);
	public event PieceComboedHandler PieceComboed;
	public void OnPieceComboed(GamePiece piece) { if (PieceComboed != null) PieceComboed(piece); }

	private void Start()
	{
		sprite = GetComponent<SpriteRenderer>();
		ConnectedPieces = new List<GamePiece>();
	}

	public void ClearState()
	{
		MatchGroup = EMPTY_MATCH_GROUP;
		if (IsMoving)
		{
			transform.localPosition = TargetPosition;
			IsMoving = false;
		}

		IsCombining = false;
		IsGarbage = false;
		
		MatchingCount = 0;
		MoveSpeed = 0f;
		
		deathCountdown = 0f;
		initialDegarbifyTime = 0f;
		degarbifyTime = 0f;
	
		// Tell any piece we're connected to that we aren't anymore
		if (ConnectedPieces != null)
		{
			foreach (var piece in ConnectedPieces)
			{
				if (piece == this)
					continue;
				else
					piece.ConnectedPieces.Remove(this);
			}
		}

		// Forget everything we're connected to
		if(ConnectedPieces != null)
			ConnectedPieces.Clear();
	}

	public bool ConnectedTo(GamePiece other)
	{
		return ConnectedPieces.Contains(other);
	}

	public void BeginCombo(float startTime)
	{
		if (IsCombining)
			return;
		IsCombining = true;

		startingColor = sprite.color;
		deathCountdown = startTime;
	}

	public void SwapTo(Vector3 targetPosition, float moveSpeed = Mathf.Infinity)
	{
		if (moveSpeed == Mathf.Infinity)
		{
			transform.localPosition = targetPosition;
			return;
		}
		TargetPosition = targetPosition;
		IsMoving = true;
		MoveSpeed = moveSpeed;
	}

	public void BeginDegarbify(int manhattenDistanceFromCleaner)
	{
		degarbifyTime = GlobalTuning.Instance.DegarbifyMinTime + GlobalTuning.Instance.DegarbifyAddTimePerDistance * manhattenDistanceFromCleaner;
		initialDegarbifyTime = degarbifyTime;
		startingColor = sprite.color;
	}

	private void Update()
	{
		UpdateCombining(Time.deltaTime);
		UpdateMoving();
		UpdateDegarbify();
	}
	
	private void UpdateDeathCountdown(float dt)
	{
		deathCountdown -= dt;
	}

	private void UpdateCombining(float dt)
	{
		if (IsCombining)
		{
			UpdateDeathCountdown(dt);

			sprite.color = Color.Lerp(startingColor, Color.white, (GlobalTuning.Instance.CombineTime - deathCountdown) / GlobalTuning.Instance.CombineTime);
			if (deathCountdown <= 0)
			{
				OnPieceComboed(this);

				// Change into an empty piece.
				IsCombining = false;
				MatchGroup = EMPTY_MATCH_GROUP;
				MotherFactory.GenerateGamePieceAppearance(this);
				GlobalTuning.Instance.OnCombineAchieved(MatchingCount);
			}
		}
	}

	private void UpdateMoving()
	{
		if (IsMoving)
		{
			transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
			if ((transform.localPosition - TargetPosition).sqrMagnitude <= Mathf.Epsilon)
			{
				// Make sure all connected pieces finish their moves ATOMICALLY.
				if (IsConnected)
					foreach (var piece in ConnectedPieces)
						piece.CompleteMovement();
				else
					CompleteMovement();
			}
		}
	}

	private void CompleteMovement()
	{
		transform.localPosition = TargetPosition;
		IsMoving = false;
	}

	private void UpdateDegarbify()
	{
		if (degarbifyTime <= 0 || !IsGarbage)
			return;

		degarbifyTime -= Time.deltaTime;
		sprite.color = Color.Lerp(startingColor, Color.white, 1 - (degarbifyTime/initialDegarbifyTime));

		if (degarbifyTime <= 0)
		{
			// Make this a new random piece!
			ConnectedPieces.Clear();
			IsGarbage = false;
			MatchGroup = MotherBoard.OtherRandomGenerator.Next(1, MotherBoard.NumPieceTypes);
			MotherFactory.GenerateGamePieceAppearance(this);
		}
	}
}
