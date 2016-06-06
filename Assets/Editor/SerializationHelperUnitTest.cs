using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System;

public class SerializationHelperUnitTest 
{	
	string testInput_ = "2c4c1c2c2c4r1c2c3c1c4c1r1c2c1c1c3c2r4c4c2c0c2c4r4c3c0c0c2c3r0c0c0c0c3c0r0c0c0c0c0c0r0c0c0c0c0c0r0c0c0c0c0c0r0c0c0c0c0c0r0c0c0c0c0c0r0c0c0c0c0c0r0c0c0c0c0c0";

	private GamePiece CreateMockGamePiece(int group) {
		var piece = new GamePiece();
		piece.MatchGroup = group;
		return piece;
	}
	
	private IPieceFactory GetFactoryMock() {
		return new MockPieceFactory();
	}	
	
	private GameBoard GetBoardMock(IPieceFactory factory) {
		var go = new GameObject();
		var board = go.AddComponent<GameBoard>();
		int rows = 13;
		int cols = 6;
		board.InitializeEmptyBoard(rows, cols, factory);
		return board;
	}
	
	[Test]
  public void TestDeserialize_BoardAlreadyCreated()
  {
		var factory = GetFactoryMock();
		var board = GetBoardMock(factory);
		SerializationHelper.DeserializeBoard(board, testInput_, factory);
	}
}

public class MockPieceFactory : IPieceFactory {
	public void MutateGamePiece(GamePiece piece, int matchGroup) {}
	public GamePiece CreateGamePiece(int MatchGroup = GamePiece.EMPTY_MATCH_GROUP) {
		var go = new GameObject();
		var piece = go.AddComponent<GamePiece>();
		piece.MatchGroup = MatchGroup;
		return piece;
	}
	public void GenerateGamePieceAppearance(GamePiece piece) {}
}