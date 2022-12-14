using UnityEngine;
using MessageTypes;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using Newtonsoft.Json;
using UnityEngine.Windows;

public class GameSession : MonoBehaviour
{
    public enum StateType
    {
        Error,
        Connecting,
        WaitingForOtherPlayer,
        WaitingForInput,
        Processing,
    }


    // exposed so explosion can get the alternate player from the player that was hit
    // to change their score, but this is hacky, wonder if there is a better way...
    // maybe global game state singleton with static variable for the state?
    public Player playerLeft;
    public Player playerRight;


    public AnimationCurve deltaCurve;
    public UIDocument ui;
    public string error { get; private set; } = ""; // set before changing state. @robust remove the setget on state for the error state, make only accessible through function
    private float processProgress = 0.0f;
    public float processSpeed { get; private set; } = 1.5f;

    private string uuid;
    private Label statusLabel;

    private Label leftScoreLabel;
    private Label rightScoreLabel;

    private bool sentMyInput = false;
    private bool receivedRemoteInput = false;
    private StateType _state = StateType.Connecting;
    public StateType State {
        get { return _state; }
        private set
        {
            if(value == StateType.WaitingForInput)
            {
                sentMyInput = false;
                receivedRemoteInput = false;
            }
            else if(value == StateType.Processing)
            {
                processProgress = 0.0f;
            }
            else if(value == StateType.Error)
            {
                Debug.Log("Switched to error: " + error);
            }
            _state = value;
        }
    }

    public delegate void OnNewInput(PlayerInput input);
    public event OnNewInput OnMyInput;
    public event OnNewInput OnRemoteInput;

    public delegate void UpdateGameState(GameState gs); // add YOUR data to the gamestate
    public delegate void FromGameState(GameState gs); // do not mutate gs

    public event UpdateGameState OnUpdateGameState;
    public event FromGameState OnFromGameState;


    private NativeWebSocket.WebSocket sock = null;
    private string lobbycode = null;
    private PlayerInput myInput;
    private PlayerInput remoteInput;


    private void SendMessage(ClientToServer message)
    {
        string toSend = Newtonsoft.Json.JsonConvert.SerializeObject(message, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        });
        Debug.Log("Sending: " + toSend);
        sock.SendText(toSend).Wait();
    }

    public void SupplyMyInput(PlayerInput input)
    {
        SendMessage(new ClientToServer
        {
            type = ClientToServer.Type.MyRoundInput,
            uuid = uuid,
            roundInput = input,
        });
        myInput = input;
        sentMyInput = true;
        MaybeDoSimulate();
    }


    private void MaybeDoSimulate()
    {
        if(sentMyInput && receivedRemoteInput)
        {
            if (OnMyInput != null)
            {
                OnMyInput(myInput);
            }
            if (OnRemoteInput != null)
            {
                OnRemoteInput(remoteInput);
            }
            State = StateType.Processing;
        }
    }

    private void PauseTime()
    {
        Time.timeScale = 0.0f;
        Time.fixedDeltaTime = 0.0f;
    }

    private void Start()
    {
        uuid = System.Guid.NewGuid().ToString();
        PauseTime();

        statusLabel = ui.rootVisualElement.Q<Label>("status");
        rightScoreLabel = ui.rootVisualElement.Q<Label>("rightscore");
        leftScoreLabel = ui.rootVisualElement.Q<Label>("leftscore");

        sock = new NativeWebSocket.WebSocket(MultiplayerConfig.url);
        State = StateType.Connecting;
        
        sock.OnOpen += () =>
        {
            if(MultiplayerConfig.host)
            {
                SendMessage(new ClientToServer
                {
                    uuid = uuid,
                    type = ClientToServer.Type.CreateLobby,
                });
            } else
            {
                SendMessage(new ClientToServer
                {
                    uuid = uuid,
                    type = ClientToServer.Type.JoinLobby,
                    lobbyToJoin = MultiplayerConfig.joiningLobbyCode,
                });
            }
            State = StateType.WaitingForOtherPlayer;
        };

        sock.OnError += (e) =>
        {
            error = "Websocket error: " + e;
            State = StateType.Error;
        };

        sock.OnMessage += (bytes) =>
        {
            string messageText = System.Text.Encoding.UTF8.GetString(bytes);
            try
            {
                ServerToClient msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerToClient>(messageText);
                switch (msg.type)
                {
                    case ServerToClient.Type.GameState:
                        //if (State != StateType.WaitingForInput) throw new System.Exception("Server gave game state but not ready for that yet");
                        
                        if(OnFromGameState != null)
                        {
                            OnFromGameState(msg.gameState);
                        }
                        State = StateType.WaitingForInput;
                        break;
                    case ServerToClient.Type.RoundInput:
                        remoteInput = msg.otherPlayerRoundInput;
                        receivedRemoteInput = true;
                        MaybeDoSimulate();
                        break;
                    case ServerToClient.Type.CreatedLobby:
                        lobbycode = msg.lobbyCode;
                        break;
                    case ServerToClient.Type.LobbyReadyToPlay:
                        if (State != StateType.WaitingForOtherPlayer) throw new System.Exception("Expected state to be Connecting but was: " + State);
                        State = StateType.WaitingForInput;
                        break;
                    case ServerToClient.Type.Error:
                        error = "Server error: " + msg.errorMessage;
                        State = StateType.Error;
                        break;
                    default:
                        error = "Unknown message type: " + msg.type;
                        State = StateType.Error;
                        break;
                }
            }
            catch(System.Exception ex)
            {
                error = "Failed to receive sockets message: " + ex.Message + "\n message text: " + messageText;
                State = StateType.Error;
            }
        };

        sock.Connect();
    }

    

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        sock.DispatchMessageQueue();
#endif
        rightScoreLabel.text = playerRight.score.ToString();
        leftScoreLabel.text = playerLeft.score.ToString();

        switch (State)
        {
            case StateType.Error:
                statusLabel.text = "Error";
                break;
            case StateType.Connecting:
                statusLabel.text = "Connecting...";
                break;
            case StateType.WaitingForOtherPlayer:
                if(MultiplayerConfig.host)
                {
                    statusLabel.text = lobbycode == null ? "Waiting for lobby to be created..." : "Waiting for other player. Lobbycode: " + lobbycode;
                } else
                {
                    lobbycode = MultiplayerConfig.joiningLobbyCode;
                    statusLabel.text = "Joining lobby " + lobbycode;
                }
                break;
            case StateType.WaitingForInput:
                statusLabel.text =  sentMyInput  ? "Waiting for remote input" : "Waiting for your input";
                PauseTime();
                break;
            case StateType.Processing:
                statusLabel.text = "Processing...";
                processProgress += Time.unscaledDeltaTime * processSpeed;
                if (processProgress > 1.0f)
                {
                    if(MultiplayerConfig.host)
                    {
                        var gs = new GameState();
                        if(OnUpdateGameState != null)
                        {
                            OnUpdateGameState(gs);
                        }
                        SendMessage(new ClientToServer
                        {
                            type = ClientToServer.Type.NewGameState,
                            uuid = uuid,
                            newGameState = gs,
                        });
                    }
                    State = StateType.WaitingForInput;
                }
                float newScale = deltaCurve.Evaluate(processProgress);
                newScale = Mathf.Max(newScale, 0.01f);
                Time.timeScale = newScale;
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
                break;
        }
    }
}