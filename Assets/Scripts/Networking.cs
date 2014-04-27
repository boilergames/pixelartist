using UnityEngine;
using System.Collections;

public class Networking : MonoBehaviour {

	public string gameVersionID;
	public GUISkin skin;
	public GameObject player;
	public GameObject gamestate;
	public Canvas canvas;
	public bool connected;
	public bool waiting;
	public bool enterPassword;
	public bool listServers;

	public string username = "Dummbeutel";

	public string serverPassword;
	public float serverRoundLength;
	public int serverPlayerCount;

	public string selectedGUID;
	public string joinPassword;

	public Gamestate instantiatedGamestate;
	public Player myPlayer;

	public Vector2 scroll;

	void Start () {

		MasterServer.ipAddress = "teamspeak.jancc.de";
		MasterServer.port = 23466;
		Network.natFacilitatorIP = "teamspeak.jancc.de";
		Network.natFacilitatorPort = 50005;

		if(PlayerPrefs.HasKey("username"))
			username = PlayerPrefs.GetString("username");
		else
			username = "Dummbeutel";

		joinPassword = "";
		serverPassword = "";
		serverRoundLength = 60.0f;
		serverPlayerCount = 8;

	}

	void OnGUI () {

		if(connected)
			return;

		GUI.skin = skin;

		GUILayout.BeginArea(new Rect(Screen.height * (96.0f / 64.0f), 0, Screen.width - Screen.height * (96.0f / 64.0f), Screen.height));
		scroll = GUILayout.BeginScrollView(scroll);

		if(enterPassword)
		{
			GUILayout.Label ("The Server is protected, please enter password");
			joinPassword = GUILayout.PasswordField(joinPassword, '*');
			if(GUILayout.Button("Join"))
			{
				listServers = false;
				enterPassword = false;
				Connect(selectedGUID);
			}
			if(GUILayout.Button("Back"))
			{
				enterPassword = false;
			}
		}
		else if(listServers)
		{
			foreach(HostData host in MasterServer.PollHostList())
			{
				GUILayout.Label(host.gameName + " " + host.connectedPlayers.ToString() + "/" + host.playerLimit.ToString());
				if(host.passwordProtected) {
					GUILayout.Label("Password Protected Game");
					if(GUILayout.Button("Join"))
					{
						selectedGUID = host.guid;
						enterPassword = true;
					}
				}
				else {
					if(GUILayout.Button("Join"))
					{
						listServers = false;
						Connect(host.guid);
					}
				}
			}
			if(GUILayout.Button("Back"))
			{
				listServers = false;
			}
		}
		else if(!waiting)
		{
			GUILayout.Label("Username:");
			username = GUILayout.TextField(username);

			if(username.Length > 16)
				username = username.Remove(16);

			if(GUILayout.Button("Find Games"))
			{
				MasterServer.RequestHostList("pixelartist" + gameVersionID);
				listServers = true;
			}

			if(GUILayout.Button("Create Game"))
				CreateServer(25565);

			GUILayout.Label("Settings for Server:");

			GUILayout.Label("Password (leave blank for none)");
			serverPassword = GUILayout.PasswordField(serverPassword, '*');
			if(serverPassword.Length > 12)
				serverPassword = serverPassword.Remove(12);

			GUILayout.Label("Duration per word: " + Mathf.RoundToInt(serverRoundLength).ToString() + "s");
			serverRoundLength = GUILayout.HorizontalSlider(serverRoundLength, 30.0f, 120.0f);

			GUILayout.Label("Max Players: " + (serverPlayerCount+1).ToString());
			serverPlayerCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(serverPlayerCount, 1.0f, 16.0f));


		}
		else
			GUILayout.Label("Please Wait...");

		GUILayout.FlexibleSpace();

		GUILayout.Label("Written and Directed by:\n" +
		                "Bernd Sawyer");

		GUILayout.EndScrollView();
		GUILayout.EndArea();

	}

	void CreateServer (int port) {

		Network.InitializeSecurity();
		Network.incomingPassword = serverPassword;
		Network.InitializeServer(serverPlayerCount, port, true);

		MasterServer.RegisterHost("pixelartist" + gameVersionID, username + "'s game");
		waiting = true;

	}

	void Connect (string guid) {

		Network.Connect(guid, joinPassword);
		waiting = true;

	}

	void SpawnPlayer () {

		PlayerPrefs.SetString("username", username);
		PlayerPrefs.Save();
		GameObject playerGo = Network.Instantiate(player, Vector3.zero, Quaternion.identity, 1) as GameObject;
		Player playerScr = playerGo.GetComponent<Player>();
		playerScr.SetName(username);
		myPlayer = playerScr;

	}

	void OnServerInitialized () {

		GameObject gamestateGO = Network.Instantiate(gamestate, Vector3.zero, Quaternion.identity, 1) as GameObject;
		instantiatedGamestate = gamestateGO.GetComponent<Gamestate>();
		instantiatedGamestate.SetInfo(serverRoundLength);
		SpawnPlayer();
		connected = true;
		waiting = false;

	}

	void OnConnectedToServer () {
		
		SpawnPlayer();
		connected = true;
		waiting = false;

	}

	void OnDisconnectedFromServer (NetworkDisconnection info) {

		connected = false;
		Application.LoadLevel(0);

	}

	void OnFailedToConnect (NetworkConnectionError error) {

		waiting = false;

	}

	void OnPlayerDisconnected(NetworkPlayer nplayer) {

		Player player = Player.FindWithNetworkPlayer(nplayer);
		if(instantiatedGamestate.currentPlayer == player) {

			instantiatedGamestate.currentPlayer = myPlayer;
			instantiatedGamestate.currentNetworkPlayer = Network.player;
			instantiatedGamestate.EndRound();

		}

		Network.RemoveRPCs(nplayer);
		Network.DestroyPlayerObjects(nplayer);
	}
	
}
