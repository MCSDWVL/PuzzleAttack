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
	public bool IsFalling { get; set; }
	public bool IsConnected { get { return ConnectedPieces != null && ConnectedPieces.Count > 0; } }
	public bool ForceFall { get; set; }

	public Vector3 TargetPosition { get; set; }
	public float MoveSpeed { get; set; }

	public GamePieceFactory MotherFactory { get; set; }
	public GameBoard MotherBoard { get; set; }

	public int MatchingCount { get; set; }

	private float deathCountdown;
	private Color startingColor;

	private SpriteRenderer sprite;

	private static float globalDeathCountdown;
	private static float globalDeathDelay;

	public List<GamePiece> ConnectedPieces { get; private set; }

	public int Row { get; set; }
	public int Col { get; set; }

	private void Start()
	{
		sprite = GetComponent<SpriteRenderer>();
		ConnectedPieces = new List<GamePiece>();
	}

	public bool ConnectedTo(GamePiece other)
	{
		return ConnectedPieces.Contains(other);
	}

	public void BeginCombo()
	{
		Debug.Log("Beginning combo");
		if (IsCombining)
			return;
		IsCombining = true;

		startingColor = sprite.color;
		deathCountdown = GlobalTuning.Instance.CombineTime;
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

	private void FixedUpdate()
	{
		if (IsMoving)
		{
			transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
			if ((transform.localPosition - TargetPosition).sqrMagnitude <= Mathf.Epsilon)
			{
				transform.localPosition = TargetPosition;
				IsMoving = false;
			}
		}
	}

	private void Update()
	{
		if (IsCombining)
		{
			if (MotherBoard.GlobalDeathDelay > 0)
				deathCountdown = Mathf.Lerp(deathCountdown, MotherBoard.GlobalDeathCountdown, (GlobalTuning.Instance.DelayOnCombo - MotherBoard.GlobalDeathDelay) / GlobalTuning.Instance.DelayOnCombo);
			else
				deathCountdown = MotherBoard.GlobalDeathCountdown;

			sprite.color = Color.Lerp(startingColor, Color.white, (GlobalTuning.Instance.CombineTime - deathCountdown)/GlobalTuning.Instance.CombineTime);
			if (deathCountdown <= 0)
			{
				// TODO: score for melding here!
				// TODO: check for touching garbage and make it normal here!
				// Change into an empty piece.
				IsCombining = false;
				MatchGroup = EMPTY_MATCH_GROUP;
				MotherFactory.GenerateGamePieceAppearance(this);
				GlobalTuning.Instance.OnCombineAchieved(MatchingCount);
			}
		}
	}
}
