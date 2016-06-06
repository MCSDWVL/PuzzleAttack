using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SerializationHelper
{
	public static char COMBO_SEPARATOR = 'g';
	public static char ROW_SEPARATOR = 'r';
	public static char COL_SEPARATOR = 'c';
	public static char PLAYER_STATE_SEPARATOR = 'p';
	public static char STATE_SEPARATOR = 's';
	public static char SEED_SEPARATOR = 'x';
	
	public static string Serialize(GameBoard board)
	{
		var ret = "";
		SerializeBoard(board, out ret);
		return ret;
	}

	public static void SerializeBoard(GameBoard board, out string ret)
	{
		int[][] asArray;
		SerializeBoard(board, out asArray);
		ret = "";

		for (var row = 0; row < asArray.Length; ++row)
		{
			ret += string.Join(SerializationHelper.COL_SEPARATOR+"", asArray[row].Select(x => x.ToString()).ToArray());
			if (row != asArray.Length - 1)
				ret += SerializationHelper.ROW_SEPARATOR;
		}
	}

	public static void SerializeBoard(GameBoard board, out int[][] ret)
	{
		ret = new int[board.ActualRows][];
		for (var row = 0; row < board.ActualRows; ++row) {
			ret[row] = new int[board.ActualCols];
			for (var col = 0; col < board.ActualCols; ++col) {
				var piece = board.GetPieceAt(row, col);
				ret[row][col] = piece.MatchGroup;
			}
		}
	}

	public static void DeserializeBoard(GameBoard board, string matchGroupsString, IPieceFactory factory)
	{
		board.ClearBoard(factory);

		var matchGroups = new int[board.ActualRows, board.ActualCols];
		var rowStrings = matchGroupsString.Split(SerializationHelper.ROW_SEPARATOR);
		for (var row = 0; row < board.ActualRows; ++row)
		{
			var colStrings = rowStrings[row].Split(SerializationHelper.COL_SEPARATOR);
			var colVals = colStrings.Select(x => int.Parse(x)).ToArray();
			for (var col = 0; col < board.ActualCols; ++col)
			{
				factory.MutateGamePiece(board.GetPieceAt(row, col), colVals[col]);
			}
		}
		board.LinkGarbage();
	}
}

