﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MainMenu : MonoBehaviour
{
	GameBoard board;
	ComboAndGarbageManager garboMan;

	private void Start()
	{
		board = GetComponent<GameBoard>();
		garboMan = GetComponent<ComboAndGarbageManager>();

		board.GameOver = true;
	}

	private void StartNewGame(string serializedState)
	{
			garboMan.StartRoundFromSerializedString(serializedState);
	}

	private void OnGUI()
	{
		if (board.GameOver)
		{
			garboMan.SerializedState = GUI.TextField(new Rect(10, 10, 200, 20), garboMan.SerializedState, 2048);
			if (GUI.Button(new Rect(10, 100, 200, 20), "Start Game"))
				StartNewGame(garboMan.SerializedState);
		}
	}
}