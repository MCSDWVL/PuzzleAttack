using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SharedGameStateManager : MonoBehaviour
{
	private void Awake()
	{
		InitializePlayerStates();
	}

	// We track each playerStrings state separately, that way we can send the information back to them.
	// This means all the information needed to keep playing is "on the wire", only one player has to
	// hold the state at a time, it just goes back and forth "hot potato" style.
	private class PerPlayerInformation
	{
		public string boardState;
		public string garboState;
		public int round;

		public bool IsValid() { return round > 0; }

		public void InitializeInvalid()
		{
			boardState = garboState = "";
			round = 0;
		}
		
		public void Deserialize(string str)
		{
			var stateStrings = str.Split(SerializationHelper.STATE_SEPARATOR);
			round = int.Parse(stateStrings[2]);
			boardState = stateStrings[0];
			garboState  = stateStrings[1];
		}

		public string Serialize()
		{
			return string.Join(SerializationHelper.STATE_SEPARATOR+"", new string[]{boardState, garboState, round.ToString()});
		}

		public string UpdateAndSerialize(int currentRound, GameBoard board, ComboAndGarbageManager garboMan)
		{
			// We might be re-serializing the other half of the hot potato object, in which case we just 
			// want to write back our values, not check with actual game objects.
			if(board)
				boardState = board.Serialize();
			if(garboMan)
				garboState  = garboMan.Serialize();
			round = currentRound;

			return Serialize();
		}
	}

	// Each has two serialized rounds stored, because either player can "go first" and play the next round.
	private PerPlayerInformation[] localPlayerStates = new PerPlayerInformation[2];
	private PerPlayerInformation[] remotePlayerStates = new PerPlayerInformation[2];

	int gameSeed;

	private void InitializePlayerStates()
	{
		for (var i = 0; i < 2; ++i)
		{
			if(localPlayerStates[i] == null)
				localPlayerStates[i] = new PerPlayerInformation();
			localPlayerStates[i].InitializeInvalid();

			if (remotePlayerStates[i] == null)
				remotePlayerStates[i] = new PerPlayerInformation();
			remotePlayerStates[i].InitializeInvalid();
		}
	}

	private int GetNumValidRoundsFromPlayerStates()
	{
		return localPlayerStates.Count(x => x.IsValid()) + remotePlayerStates.Count(x => x.IsValid());
	}

	public string SerializeOnEndOfCurrentRound(GameBoard board, ComboAndGarbageManager garboManager)
	{
		// Player two has: 1 2, player one has: 1 -1, player one finished a round:
		//		Discard round 1s
		//    Move P22 to P21
		//    UpdateAndSerialize current to P11

		// Player two has: 1 -1, player one has: 1 -1
		//    UpdateAndSerialize current to P12

		// Serialized state will either have 1 round (first player moved but second player hasn't yet), 2 rounds (both playerStrings completed first round), or 3 (both did r1, one did r2).  Never 4 rounds.
		var numValidRounds = GetNumValidRoundsFromPlayerStates();

		// If we already have 3 valid rounds we need to drop the two starting ones, move back the remaining opponent round, and write ours to our second position.
		if(numValidRounds == 3)
		{
			// Move all second round info into first round info.
			localPlayerStates[0] = localPlayerStates[1];
			remotePlayerStates[0] = remotePlayerStates[1];

			// Invalidate second round info.
			localPlayerStates[1].InitializeInvalid();
			remotePlayerStates[1].InitializeInvalid();
		}

		// If we have at least 2 valid rounds now we want to write to second round, otherwise to first.
		var ourCurrentRound = localPlayerStates[0].round;
		localPlayerStates[numValidRounds >= 2 ? 1 : 0].UpdateAndSerialize(ourCurrentRound + 1, board, garboManager);

		// UpdateAndSerialize all 4 states together.
		var localStates = string.Join(SerializationHelper.PLAYER_STATE_SEPARATOR + "", localPlayerStates.Select(x => x.Serialize()).ToArray());
		var remoteStates = string.Join(SerializationHelper.PLAYER_STATE_SEPARATOR + "", remotePlayerStates.Select(x => x.Serialize()).ToArray());

		// Always serialize our states such that when the remote player receives the string, her states are first.
		gameSeed = board.RandomSeed;
		var retString = "" + gameSeed + SerializationHelper.SEED_SEPARATOR + remoteStates + SerializationHelper.PLAYER_STATE_SEPARATOR + localStates;
		Debug.Log("GameSeed " + gameSeed);
		Debug.Log("Returning " + retString);
		return retString;
	}

	public void Deserialize(string serializedGameState)
	{
		InitializePlayerStates();
		
		// Get the seed
		var topParts = serializedGameState.Split(SerializationHelper.SEED_SEPARATOR);
		gameSeed = int.Parse(topParts[0]);

		// Split the rounds
		var stateStrings = topParts[1].Split(SerializationHelper.PLAYER_STATE_SEPARATOR);
		for(var stateIdx = 0; stateIdx < stateStrings.Length; ++stateIdx)
		{
			// 2 of our states followed by 2 of their states/
			if(stateIdx < 2)
				localPlayerStates[stateIdx].Deserialize(stateStrings[stateIdx]);
			else
				remotePlayerStates[stateIdx-2].Deserialize(stateStrings[stateIdx]);
		}
	}

	// Load the player state for both playerStrings from the "hot potato" state string.
	public bool LoadHeadRound(GameBoard board, ComboAndGarbageManager garboMan)
	{
		// Only allow play if other player has a round for us or no rounds are ready yet
		// L1L2 R1R2
		// LOAD BLANK IF: we have no rounds in the hot potato
		// oooo -> fresh game, load blank board and no garbage
		// ooxo -> opp has 1 game and we have none, load blank board and no garbage
		//
		// REFUSE TO PLAY IF: our # rounds is > op num rounds in hot potato (equal is ok)
		// xooo -> we have one round and op has none, don't play
		// xxxo -> we have two rounds and op has one, don't play
		//
		// LOAD OP HEAD GARBAGE AND OUR HEAD BOARD IF: our # rounds is <= op num rounds
		// xoxo -> we have one round and op has one, load his garbage and our board
		// xoxx -> we have one round and op has two, load his latest garbage and our board
		var localNumRounds = localPlayerStates.Count(x => x.IsValid());
		var remoteNumRounds = remotePlayerStates.Count(x => x.IsValid());

		Debug.Log("Local num rounds " + localNumRounds);
		Debug.Log("Remote num rounds " + remoteNumRounds);

		if (localNumRounds == 0)
		{
			board.ClearBoard();
			board.InitializeFromRemote();
			garboMan.Clear();
			board.GameOver = false;
			return true;
		}
		else if (localNumRounds <= remoteNumRounds)
		{
			// get our head board string.
			board.RandomSeed = gameSeed;
			var localHead = localPlayerStates[localNumRounds - 1];
			var remoteHead = remotePlayerStates[remoteNumRounds - 1];
			var headBoard = localHead.boardState;
			var headGarbage = remoteHead.garboState;
			board.DeserializeBoard(headBoard);
			garboMan.Deserialize(headGarbage);
			board.GameOver = false;
			return true;
		}
		else if (localNumRounds > remoteNumRounds)
		{
			return false;
		}

		// uh oh!?
		return false;
	}
}