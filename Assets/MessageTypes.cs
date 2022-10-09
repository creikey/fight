
using System.Numerics;
namespace MessageTypes
{
    public class GameState
    {
        public class Player
        {
            public Vector2 position;
            public Vector2 velocity;
        }

        public Player[] players;
    }

    public class PlayerInput
    {
        public Vector2 movementDirection;
    }

    public class ClientToServer
    {
        public enum Type
        {
            JoinLobby,
            CreateLobby,
            MyRoundInput,
        }

        public string action = "OnMessage";

        public Type type;
        public string uuid;

        // wish there was a better way to do discriminated unions like this

        // JoinLobby
        public string lobbyToJoin;

        // CreateLobby

        // MyRoundInput
        public PlayerInput roundInput;
    }

    public class ServerToClient
    {
        public enum Type
        {
            Error,
            GameState,
            RoundInput,
            CreatedLobby,
            LobbyReadyToPlay,
        }
        public Type type;

        // GameState
        public GameState gameState;

        // RoundInput
        public PlayerInput otherPlayerRoundInput;

        // CreatedLobby
        public string lobbyCode;

        // LobbyReadyToPlay

        // Error
        public string errorMessage;
    }
}