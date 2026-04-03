using Godot;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class NetworkAdapter : Node
    {
        private const int Port = 7777;
        private const int MaxClients = 4;
        
        public ENetMultiplayerPeer Peer { get; private set; }

        public override void _Ready()
        {
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
        }

        public void HostGame()
        {
            Peer = new ENetMultiplayerPeer();
            var error = Peer.CreateServer(Port, MaxClients);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Failed to host: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = Peer;
            GD.Print("Server hosted successfully.");
            
            RegisterPlayer(1);
        }

        public void JoinGame(string address)
        {
            Peer = new ENetMultiplayerPeer();
            var error = Peer.CreateClient(address, Port);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Failed to join: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = Peer;
            GD.Print("Connecting to server...");
        }

        private void OnPeerConnected(long id)
        {
            GD.Print($"Peer connected: {id}");
            if (Multiplayer.IsServer())
            {
                RegisterPlayer(id);
            }
        }

        private void OnPeerDisconnected(long id)
        {
            GD.Print($"Peer disconnected: {id}");
        }

        private void RegisterPlayer(long id)
        {
            GD.Print($"Registering player {id} in game.");
        }
        
        public void StartGameAsHost()
        {
            if (Multiplayer.IsServer())
            {
                Rpc(MethodName.RpcLoadGameScene);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
        private void RpcLoadGameScene()
        {
            GD.Print("Loading GameMap scene...");
            GetTree().ChangeSceneToFile("res://Scenes/GameMap.tscn");
        }
    }
}
