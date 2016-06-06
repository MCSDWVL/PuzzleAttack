using UnityEngine;
using System.Collections.Generic;

public class GlobalDeathTimeUpdater : BoardUpdater 
{
	public float GlobalDeathCountdown { get; set; }
	public float GlobalDeathDelay { get; set; }
	
	private void Start() {
		ResetGlobalCountdown();
	}
	
	private void ResetGlobalCountdown() {
		GlobalDeathCountdown = GlobalTuning.Instance.CombineTime;
	}

	public override IEnumerable<BoardAction> UpdateBoard(GameBoard board, float dt) 
	{
		// Check if any pieces are combining.
		var anyPieceCombining = false;
		for (var r = 0; r < board.ActualRows; ++r)
		{
			for (var c = 0; c < board.ActualCols; ++c)
			{
				var piece = board.GetPieceAt(r, c);
				if (piece.IsCombining) {
					anyPieceCombining = true;
					break;
				}
			}
		}
		
		// Update the countdowns.
		if (anyPieceCombining) {
			if (GlobalDeathDelay > 0)
				GlobalDeathDelay -= dt;
			else
				GlobalDeathCountdown -= dt;	
		} else {
			ResetGlobalCountdown();
		}
		
		// Return the default (empty) list.
		return base.UpdateBoard(board, dt);
	}	
}