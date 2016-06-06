using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class GameboardUnitTest {
	/* to test:
			- ActualRows
			- ActualCols
			- GetPieceAt
			- SetPieceAt
			- InitializeFromRemote
			- ClearBoard
			- LocalPositionAt
			
			- Split these?
				- GetNeighborPosition
				- GetNeighbor
				- GetNeighbors
			
			x These have to do with how pieces move, should be split.
				x PerformSwap
				x PieceCanFall
				x CheckForFalls
				x CauseFall
				
			x These have to do with rules of combining, should be split:
				x OnPieceComboed
				x CountMatches
				x CheckForCombos
				
			- These have to do with specific "special" blocks,
				It should be much more modular for this specific game, not to mention 
				others.
				- GetRandomFactorForGarbageColumns
				- InsertGarbageRandomly
				- InsertGarbagePiece
							
			- These are very very specific to one implementation of one game:
				- GlobalDeathCountdown
				- GlobalDeathDelay
				
			- These have to do with specific timer mechanics, should be split:
				- UpdateTargetPositionForScrolling
				- MoveUpOneRow
				
			- This should be made generic for kinds of "board modifiers" in progress
				- IsInSteadyState
			
			- These should be trimmed and split to a different class:
				- Serialize
				- SerializeBoard
				- SerializeBoard
				- DeserializeBoard
			- 
	 */
	 
	/*
    [Test]
    public void EditorTest()
    {
        //Arrange
        var gameObject = new GameObject();

        //Act
        //Try to rename the GameObject
        var newGameObjectName = "My game object";
        gameObject.name = newGameObjectName;

        //Assert
        //The object has a new name
        Assert.AreEqual(newGameObjectName, gameObject.name);
    }
		*/
}
