
using System.Numerics;

namespace MessageTypes
{
    public class GameState
    {
        // default constructor must not have null values for players and properties, so can be modified in place
        // by passing by reference. It is what it is

        public class Player
        {
            public Vector2 position = new Vector2();
            public Vector2 velocity = new Vector2();
        }


        public Player playerLeft = new Player();
        public Player playerRight = new Player();
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
            NewGameState,
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

        // NewGameState
        public GameState newGameState;
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