namespace FlowVR
{
    using Fusion;
    using Fusion.Sockets;
    using System.Collections.Generic;
    using System;
    using UnityEngine;

    /// <summary>
    /// The Runner for FlowVR. Handles network events.
    /// </summary>
    [HelpURL("https://github.com/rxxyn/FlowVR/wiki")]
    public class FlowVRRunner : SimulationBehaviour, INetworkRunnerCallbacks
    {
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (FlowVRPlayer.Instance != null || !runner.IsRunning) return;

            if (player == FlowVRManager.Runner.LocalPlayer)
            {
                Debug.LogWarning("Spawning player!");
                NetworkObject NetworkedPlayer = runner.Spawn(FlowVRManager.Manager.PlayerPrefab, Vector3.zero, Quaternion.identity, player);
                FlowVRManager.Runner.SetPlayerObject(player, NetworkedPlayer);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => Debug.Log($"{nameof(OnShutdown)}: {nameof(shutdownReason)}: {shutdownReason}");
        private void Resume(NetworkRunner runner) => Debug.Log("Resumed game.");
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => Debug.Log($"MonkeNetworkRunner: {nameof(OnConnectFailed)}: {nameof(reason)}: {reason}");
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}