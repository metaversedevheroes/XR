using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Collections.Generic;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runnerPrefab;
    public GameObject playerPrefab;

    private NetworkRunner runnerInstance;

    void Start()
    {
        runnerInstance = Instantiate(runnerPrefab);
        runnerInstance.name = "NetworkRunner";

        runnerInstance.ProvideInput = true;

        runnerInstance.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "MyVRRoom",
            SceneManager = runnerInstance.GetComponent<NetworkSceneManagerDefault>(),
            PlayerCount = 10
        });

        runnerInstance.AddCallbacks(this);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        }
    }

    // 필수 콜백들 (빈 함수로 유지)
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
