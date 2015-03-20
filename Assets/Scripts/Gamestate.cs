using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Gamestate : MonoBehaviour {

	public string currentWord;
	public float roundLength;
	public float timeLeft;
	public bool inRound;
	public NetworkPlayer currentNetworkPlayer;
	public Player currentPlayer;
	public bool given30SecondWarning;
	public bool given10SecondWarning;
	public bool given5SecondWarning;
	public string playersWhoGotIt;
	public int playerGotItAmount;

	public string[] chat;
	public int chat_max_count = 32;
	public Vector2 chatScroll;

	int lastDrawingPlayer;
	float nextRoundCountdown;

	ArrayList wordsWeHadAlready;

	void Start ()
	{
		wordsWeHadAlready = new ArrayList();
		playersWhoGotIt = "";
	}

	public void EmptyPlayersWhoGotIt ()
	{
		playersWhoGotIt = "";
		playerGotItAmount = 0;
	}

	public bool didPlayerGetItAlready (string player)
	{
		return playersWhoGotIt.Contains(player);
	}

	public void playerGotIt (string player)
	{
		playersWhoGotIt += player + ",";
		playerGotItAmount++;
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
		if(playerGotItAmount >= players.Length - 1) {
			EndRound();
		}
	}

	public string getRandomWord()
	{

		bool done = false;
		string word = "";

		while(!done) {
			TextAsset text = Resources.Load("words") as TextAsset;
			string[] words = text.text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
			word = words[ UnityEngine.Random.Range(0, words.Length) ];

			if(!wordsWeHadAlready.Contains(word))
				done = true;
		}

		wordsWeHadAlready.Add(word);
		if(wordsWeHadAlready.Count > 50)
			wordsWeHadAlready.RemoveAt(0);

		return word;

	}

	void Update ()
	{
		if(Network.isClient && inRound)
			timeLeft -= Time.deltaTime;

		if(!Network.isServer)
			return;

		if(inRound)
		{
			timeLeft -= Time.deltaTime;
			nextRoundCountdown = 15.0f;
			if(timeLeft <= 30.0f && !given30SecondWarning)
			{
				currentPlayer.PostMessage("[SERVER] 30 Seconds Left!", currentNetworkPlayer, true, true);
				given30SecondWarning = true;
			}
			else if(timeLeft <= 10.0f && !given10SecondWarning)
			{
				currentPlayer.PostMessage("<color=red>[SERVER] 10 Seconds Left!</color>", currentNetworkPlayer, true, true);
				given10SecondWarning = true;
			}
			else if(timeLeft <= 5.0f && !given5SecondWarning)
			{
				currentPlayer.PostMessage("<b><color=red>[SERVER] 5 Seconds Left!</color></b>", currentNetworkPlayer, true, true);
				given5SecondWarning = true;
			}
			else if(timeLeft <= 0.0f)
			{
				EndRound();
			}
		}
		else
		{
			nextRoundCountdown -= Time.deltaTime;
		}

		if(nextRoundCountdown <= 0.0f && !inRound) {

			GameObject[] playerGOs = GameObject.FindGameObjectsWithTag("Player");

			if(playerGOs.Length > 1) {

				lastDrawingPlayer++;
				if(lastDrawingPlayer > playerGOs.Length - 1)
					lastDrawingPlayer = 0;
				Player player = playerGOs[lastDrawingPlayer].GetComponent<Player>();
				BeginRound(player.GetComponent<NetworkView>().viewID.owner, player.GetComponent<NetworkView>().viewID, getRandomWord());

			}

			nextRoundCountdown = 15.0f;

		}
	}

	public void SetInfo(float newRoundLength) {
		GetComponent<NetworkView>().RPC ("SetInfoRPC", RPCMode.AllBuffered, newRoundLength);
	}

	[RPC]
	public void SetInfoRPC(float newRoundLength) {
		roundLength = newRoundLength;
	}

	public void BeginRound (NetworkPlayer player, NetworkViewID viewId, string word)
	{

		GetComponent<NetworkView>().RPC("BeginRoundRPC", RPCMode.All, player, viewId, word);

	}

	[RPC]
	void BeginRoundRPC (NetworkPlayer player, NetworkViewID viewId, string word)
	{
		currentNetworkPlayer = player;
		currentPlayer = NetworkView.Find(viewId).GetComponent<Player>();

		inRound = true;
		currentWord = word;

		currentPlayer.canvas.Cleanup();

		timeLeft = roundLength;

		if(Network.isServer)
		{
			given30SecondWarning = false;
			given10SecondWarning = false;
			given5SecondWarning = false;
			currentPlayer.PostMessage("<color=red>[SERVER] Next Round Started! <b>" + currentPlayer.myName + "</b> has to draw!</color>", currentNetworkPlayer, true, true);
			currentPlayer.PostMessage("[SERVER] Your word is: <color=red>" + word + "</color>", currentNetworkPlayer, true, true, true);
		}
	}

	public void EndRound ()
	{
		
		GetComponent<NetworkView>().RPC("EndRoundRPC", RPCMode.All);
		
	}
	
	[RPC]
	void EndRoundRPC ()
	{
		inRound = false;
		bool falsePlayer = false;

		if(!currentPlayer)
		{
			falsePlayer = true;
			currentPlayer = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
			currentNetworkPlayer = Network.player;
		}

		if(Network.isServer)
		{
			currentPlayer.PostMessage("<color=red>[SERVER] Round is over! The word was: <b>"+ currentWord + "</b></color>", currentNetworkPlayer, true, true);

			if(playersWhoGotIt == "")
				currentPlayer.PostMessage("<color=red>[SERVER] No one got it :(</color>", currentNetworkPlayer, true, true);

			string[] gotIt = playersWhoGotIt.Split(',');
			for(int i = 0; i < gotIt.Length; i++) {

				if(i > 0 && gotIt[i] != "")
				{
					currentPlayer.PostMessage("<color=green>[SERVER] <b>" + gotIt[i] + "</b> gets a point!</color>", currentNetworkPlayer, true, true);
					Player.FindWithName(gotIt[i]).AddScore(1);
					if(!falsePlayer)
						currentPlayer.AddScore(1);
				}
				else if(i == 0 && gotIt[i] != "")
				{
					currentPlayer.PostMessage("<color=green>[SERVER] <b>" + gotIt[i] + "</b> got it first!</color>", currentNetworkPlayer, true, true);
					Player.FindWithName(gotIt[i]).AddScore(3);
					if(!falsePlayer)
						currentPlayer.AddScore(1);
				}

			}
		}

		EmptyPlayersWhoGotIt();
	}

}
