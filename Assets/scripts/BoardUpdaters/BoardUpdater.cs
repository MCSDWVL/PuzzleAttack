using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BoardUpdater : MonoBehaviour 
{
	public virtual IEnumerable<BoardAction> UpdateBoard(GameBoard board, float dt){
		return Enumerable.Empty<BoardAction>();
	}
	
	public virtual bool WouldUpdateBoard(GameBoard board) {
		return true;
	}
}