using UnityEngine;
using System;
using System.Collections;

public class TestUtil : MonoBehaviour {

	public static IEnumerator WaitForBoardSteadyState(GameBoard board, Action callback)
	{
		while (!board.IsInSteadyState())
			yield return new WaitForEndOfFrame();

		callback();
	}
}
