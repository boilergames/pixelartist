using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

	public GUISkin skin;

	public Canvas canvas;
	public string myName;
	public int score;
	public Gamestate gamestate;

	public Color[] definedColors;
	public string[] definedColorNames;
	public int selectedDrawColor;

	public string chatMessage;
	public string connected;
	public int lastPlayerNumber;
	public float playerUpdateFrequency;
	float nextPlayerUpdate;

	bool requestChatMessage;
	float chatMessageTimer;

	public Vector2 lastMousePos;
	public Vector2 curMousePos;

	// Use this for initialization
	void Start () {

		GameObject gamestateGO = GameObject.FindGameObjectWithTag("Gamestate");
		gamestate = gamestateGO.GetComponent<Gamestate>();

		GameObject canvasGO = GameObject.FindGameObjectWithTag("MainCamera");
		canvas = canvasGO.GetComponent<Canvas>();

		if(!networkView.isMine)
			return;

		gamestate.chat = new string[gamestate.chat_max_count];

		for(int i = 0; i < gamestate.chat_max_count; i++)
		{
			gamestate.chat[i] = "";
		}

		lastMousePos = Vector2.zero;
		curMousePos = Vector2.zero;

	}

	public static Player FindWithNetworkPlayer(NetworkPlayer nplayer) {

		GameObject[] playerGOs = GameObject.FindGameObjectsWithTag("Player");

		for(int i = 0; i < playerGOs.Length; i++)
		{
			Player player = playerGOs[i].GetComponent<Player>();
			if(player.networkView.viewID.owner == nplayer) {
				return player;
			}

		}

		return null;

	}

	public static Player FindWithName(string searchName) {
		
		GameObject[] playerGOs = GameObject.FindGameObjectsWithTag("Player");
		
		for(int i = 0; i < playerGOs.Length; i++)
		{
			Player player = playerGOs[i].GetComponent<Player>();
			if(player.myName == searchName) {
				return player;
			}
			
		}
		
		return null;
		
	}

	// Update is called once per frame
	void FixedUpdate () {
	
		if(!networkView.isMine)
			return;

		lastMousePos = curMousePos;
		curMousePos = Input.mousePosition;

		double fx1 = curMousePos.x / (Screen.width * (0.75));
		double fy1 = curMousePos.y / Screen.height;
		int x1 = Mathf.RoundToInt((float)(fx1 * 95.0));
		int y1 = Mathf.RoundToInt((float)(fy1 * 64.0));

		double fx2 = lastMousePos.x / (Screen.width * (0.75));
		double fy2 = lastMousePos.y / Screen.height;
		int x2 = Mathf.RoundToInt((float)(fx2 * 95.0));
		int y2 = Mathf.RoundToInt((float)(fy2 * 64.0));

		if(!(x1 > 95 || y1 > 63 || x1 < 0 || y1 < 0))
		{
			if(Input.GetMouseButton(0))
			{
				SetPixel(x1,y1,x2,y2,definedColors[selectedDrawColor]);
			}
			if(Input.GetMouseButton(1))
			{
				float mod = Random.Range(0.98f, 1.0f);
				Color clearColor = new Color(mod, mod, mod);
				SetPixel(x1,y1,x2,y2,clearColor);
			}
		}

		if(nextPlayerUpdate < Time.time)
		{
			GameObject[] playerGOs = GameObject.FindGameObjectsWithTag("Player");
			if(/*playerGOs.Length != lastPlayerNumber*/true)
			{
				connected = "Players: ";

				for(int i = 0; i < playerGOs.Length; i++)
				{
					Player player = playerGOs[i].GetComponent<Player>();
					connected += player.myName.ToString();
					connected += " (" + player.score + "), ";
				}

				lastPlayerNumber = playerGOs.Length;
			}
			nextPlayerUpdate = Time.time + playerUpdateFrequency;
		}

		if(chatMessageTimer >= 0.0f)
			chatMessageTimer -= Time.deltaTime;

		if(requestChatMessage)
		{
			PostChatMessage();
			requestChatMessage = false;
		}

	}

	void PostChatMessage () {
		if(chatMessageTimer > 0.0f || chatMessage.Length < 1)
			return;

		PostMessage(chatMessage, Network.player);
		chatMessage = "";
		chatMessageTimer = 1.0f;
	}

	void SetPixel (int x1, int y1, int x2, int y2, Color color) {

		if(gamestate.inRound && gamestate.currentNetworkPlayer == Network.player)
			networkView.RPC("SetPixelRPC", RPCMode.All, x1,y1,x2,y2,color.r,color.g,color.b);
		
	}
	
	[RPC]
	void SetPixelRPC (int x1, int y1, int x2, int y2, float r, float g, float b) {

		if(gamestate.currentNetworkPlayer == Network.player);
			canvas.SetPixel(x1,y1,x2,y2,r,g,b);

	}

	void CleanCanvas () {

		if(gamestate.inRound && gamestate.currentNetworkPlayer == Network.player)
			networkView.RPC("CleanCanvasRPC", RPCMode.All);
		
	}
	
	[RPC]
	void CleanCanvasRPC () {
		
		canvas.Cleanup();

	}

	public void SetName (string newName) {

		networkView.RPC("SetNameRPC", RPCMode.AllBuffered, newName);
		
	}

	[RPC]
	void SetNameRPC (string newName) {

		if(Network.isServer && Player.FindWithName(newName) != null)
		{
			SetName(newName + Random.Range(0, 1000).ToString());
			return;
		}

		newName = newName.Replace(',', ' ');
		newName = newName.Replace('<', '>');
		if(myName.Length > 16 || myName == "")
			myName = "Fotzenhobel";
		myName = newName;

	}

	public void AddScore (int added) {

		networkView.RPC("AddScoreRPC", RPCMode.AllBuffered, added);

	}

	[RPC]
	void AddScoreRPC (int added) {

		score += added;

	}

	public void PostMessage (string message, NetworkPlayer sender, bool anonymously = false, bool allowRTF = false, bool visibleOnlyToSender = false) {

		if(Network.isServer && !anonymously) {

			if(message.StartsWith("/kick ")) {

				message = message.Replace("/kick ", "");
				Player player = Player.FindWithName(message);
				if(!player)
					return;
				Network.CloseConnection(player.networkView.viewID.owner, true);
				return;

			}

		}

		networkView.RPC("PostMessageRPC", RPCMode.All, message, sender, anonymously, allowRTF, visibleOnlyToSender);
		
	}

	[RPC]
	void PostMessageRPC (string message, NetworkPlayer sender, bool anonymously, bool allowRTF, bool visibleOnlyToSender) {

		if(gamestate.inRound)
		{
			if(gamestate.currentNetworkPlayer == sender && !anonymously)
				return;

			if(message.ToLower() == gamestate.currentWord)
			{
				if(Network.isServer)
				{
					if(!gamestate.didPlayerGetItAlready(myName))
					{
						gamestate.playerGotIt(myName);
						PostMessage(myName + " scored!!", sender, true, true);
						return;
					}
					else
						return;
				}
				else
					return;
			}
		}

		if(visibleOnlyToSender && sender != Network.player)
			return;

		string finalstring = "";

		if(!anonymously)
			finalstring += myName + ": ";

		if(!allowRTF && message.Contains(">"))
		{
			message = message.Replace('>', '<');
		}

		finalstring += message;

		ChatPush(finalstring);

	}

	void ChatPush (string message) {

		for(int i = gamestate.chat_max_count - 1; i > 0; i--)
		{
			gamestate.chat[i] = gamestate.chat[i - 1];
		}

		gamestate.chat[0] = message;

		gamestate.chatScroll.y = Mathf.Infinity;

	}

	void OnGUI () {

		if(!networkView.isMine)
			return;

		GUI.skin = skin;

		if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return &&
		   GUI.GetNameOfFocusedControl() == "Chatfield") {
			
			requestChatMessage = true;
			
		}

		Rect menuPos = new Rect(Screen.height * (96.0f / 64.0f), 0, Screen.width - Screen.height * (96.0f / 64.0f), Screen.height - 256);

		GUILayout.BeginArea(menuPos);
		selectedDrawColor = GUILayout.SelectionGrid(selectedDrawColor, definedColorNames, 3);

		if(GUILayout.Button ("Cleanup"))
			CleanCanvas();

		if(gamestate.currentPlayer == this && gamestate.inRound) {

			GUILayout.Label("Your word: " + gamestate.currentWord);
			float percentage = gamestate.timeLeft / gamestate.roundLength;
			GUILayoutOption width = GUILayout.Width( menuPos.width * percentage );
			GUI.color = Color.Lerp(Color.red, Color.green, percentage);
			GUILayout.Box (Mathf.RoundToInt(gamestate.timeLeft).ToString() + "s", width);
			GUI.color = Color.white;

		}

		GUILayout.FlexibleSpace();

		GUILayout.Label(connected);

		if(Network.isServer) {
			if(GUILayout.Button ("Close Server"))
				Network.Disconnect();
		}
		else {
			if(GUILayout.Button ("Disconnect"))
				Network.Disconnect();
		}

		GUILayout.EndArea();

		GUI.Box (new Rect(menuPos.x, Screen.height - 256, menuPos.width, 256 - 24), "");

		GUILayout.BeginArea(new Rect(menuPos.x, Screen.height - 256, menuPos.width, 256 - 24));
		gamestate.chatScroll = GUILayout.BeginScrollView(gamestate.chatScroll);
		for(int i = gamestate.chat_max_count - 1; i >= 0; i--)
		{
			if(gamestate.chat[i] != "")
				GUILayout.Label (gamestate.chat[i]);
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();

		GUI.SetNextControlName("Chatfield");
		chatMessage = GUI.TextField (new Rect(menuPos.x, Screen.height - 24, menuPos.width - 64, 24), chatMessage);
		if(chatMessage.Length > 64)
			chatMessage = chatMessage.Remove(64);
		if(chatMessageTimer <= 0.0f && GUI.Button(new Rect(Screen.width - 64, Screen.height - 24, 64, 24), "Send") && chatMessage.Length > 0)
		{
			PostChatMessage();
		}
	
	}

}
