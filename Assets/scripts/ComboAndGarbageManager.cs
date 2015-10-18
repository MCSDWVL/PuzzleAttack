using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ComboAndGarbageManager : MonoBehaviour
{
	private int[] inputGarbageTimes;
	private int[] comboOnGivenRow;
	private int currentRowInChunk = 0;
	private GameBoard board;
	private SharedGameStateManager serializer;
	
	public string SerializedState;

	private void Start()
	{
		Clear();
		board = GetComponent<GameBoard>();
		serializer = GetComponent<SharedGameStateManager>();
		RegisterEventHandlers();
	}

	private void OnDestroy()
	{
		UnregisterEventHandlers();
	}

	private void RegisterEventHandlers()
	{
		board.RowAdded += OnRowAdded;
		board.TotalCombo += OnTotalCombo;
	}

	private void UnregisterEventHandlers()
	{
		board.RowAdded -= OnRowAdded;
		board.TotalCombo -= OnTotalCombo;
	}

	private void OnRowAdded(GameBoard board)
	{
		++currentRowInChunk;
		if (inputGarbageTimes != null && inputGarbageTimes.Length > currentRowInChunk && inputGarbageTimes[currentRowInChunk] > 0)
		{
			// Trigger Garbage Creation
			board.InsertGarbageRandomly(inputGarbageTimes[currentRowInChunk]);
		}

		if (currentRowInChunk >= GlobalTuning.Instance.RowsPerGameChunk)
		{
			board.GameOver = true;
			SerializedState = serializer.SerializeOnEndOfCurrentRound(board, this);
		}
	}

	public bool DoDeserialize = false;
	private void Update()
	{
		if (DoDeserialize)
		{
			DoDeserialize = false;
			StartRoundFromSerializedString(SerializedState);
		}
	}

	public void StartRoundFromSerializedString(string serializedString)
	{
		if(serializedString != null && serializedString != "")
			serializer.Deserialize(serializedString);

		serializer.LoadHeadRound(board, this);
	}

	private void OnTotalCombo(GameBoard board, int count)
	{
		comboOnGivenRow[currentRowInChunk] += count;
	}

	public void Deserialize(string comboCount)
	{
		Clear();
		inputGarbageTimes = comboCount.Split(SerializationHelper.COMBO_SEPARATOR).Select(x => int.Parse(x)).ToArray();
	}

	public void Clear()
	{
		comboOnGivenRow = new int[GlobalTuning.Instance.RowsPerGameChunk];
		inputGarbageTimes = new int[GlobalTuning.Instance.RowsPerGameChunk];
		currentRowInChunk = 0;
	}

	public string Serialize()
	{
		return string.Join(SerializationHelper.COMBO_SEPARATOR+"", comboOnGivenRow.Select(x => x.ToString()).ToArray());
	}
}