using UnityEngine;
using System.Collections;

public class GlobalTuning : MonoBehaviour
{
	public static GlobalTuning Instance { get; private set; }
	public float SwapSpeed = 1f;
	public float FallSpeed = 1f;
	public float CombineTime = 1f;
	public int MinMatchingPieces = 4;
	public float DelayOnCombo = .3f;
	public float SecondsPerRowAdd = 3f;
	public float MinimumSecondsPerRow = 0f;
	public float SubtractSecondsPerRowOnEachCombine = 0f;
	public float SubtractSecondsPerRowOnEachNewRow = 0f;
	public int RowsPerGameChunk = 12;

	public float DegarbifyMinTime = 1;
	public float DegarbifyAddTimePerDistance = 1;

	public void SubtractSecondsPerRow(float subtractAmt)
	{
		SecondsPerRowAdd -= subtractAmt;
		SecondsPerRowAdd = Mathf.Max(SecondsPerRowAdd, MinimumSecondsPerRow);
	}

	public void OnRowAdded()
	{
		SubtractSecondsPerRow(SubtractSecondsPerRowOnEachNewRow);
	}

	public void OnCombineAchieved(int numInGrop)
	{
		// TODO: should this scale?
		SubtractSecondsPerRow(SubtractSecondsPerRowOnEachCombine);
	}

	private void Awake()
	{
		Instance = this;
	}
}
