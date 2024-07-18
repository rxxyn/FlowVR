namespace FlowVR
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Fusion;
    using System.Linq;
    using Fusion.Photon.Realtime;
    using Photon.Voice;
    using System.Text;
    using UnityEngine.SceneManagement;
    using TMPro;
    using System.Collections;
    using UnityEditor;
    using Photon.Voice.Fusion;
    using Photon.Voice.Unity;

    /// <summary>
    /// A class that manages main operations for FlowVR.
    /// </summary>
    [HelpURL("https://github.com/rxxyn/FlowVR/wiki/Get-Started")]
    public class FlowVRManager : MonoBehaviour
    {
        public static FlowVRManager Manager { get; private set; }
        public static NetworkRunner Runner { get; private set; }

        public roomData RoomData { get; private set; }

        [Space(20)]
        [Header("All Photon settings need to be set in Photon Settings, not here. \nTools > Fusion > Realtime Settings")]

        [Space(20)]
        [Header("Player")]
        public Transform Head;
        public Transform LeftHand;
        public Transform RightHand;
        public Color DefaultColor = Color.white;
        public string DefaultName = "Player";

        [Header("Room Settings")]

        [Tooltip("How many people can connect to a single room?")]
        [SerializeField] private int MaxPlayerCount = 10;

        [Tooltip("The default queue.")]
        public string DefaultQueue = "Default";

        [Header("Connection Settings")]

        [Tooltip("The prefab for the Runner and Voice.")] public GameObject RunnerAndVoice;
        [Tooltip("The prefab for the Player.")] public NetworkPrefabRef PlayerPrefab;
        [Tooltip("Connect when the game starts?")]
        [SerializeField] private bool ConnectOnAwake = true;

        private const GameMode ConnectionMode = GameMode.Shared;

        private void Start()
        {
            if (Manager == null)
                Manager = this;
            else
            {
                Application.Quit();
            }

            if (Runner == null)
            {
                Runner = FindObjectOfType<NetworkRunner>();
                if (Runner == null)
                    CreateNewRunner();
            }

            if (string.IsNullOrEmpty(Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdFusion) || string.IsNullOrEmpty(Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdVoice) || string.IsNullOrEmpty(Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.FixedRegion))
            {
                throw new Exception("The Photon settings have not been set correctly! AppIdFusion, AppIdVoice, or FixedRegion was left null! Please check your Photon Settings by navigating to Tools > Photon > Realtime Settings.\n For more help, go to https://github.com/rxxyn/FlowVR/wiki/Get-Started");
            }

            if (ConnectOnAwake)
            {
                JoinRandomRoom(DefaultQueue);
            }
        }

        private static void CreateNewRunner()
        {
            if (Runner == null)
            {
                GameObject NewRunner = Instantiate(Manager.RunnerAndVoice, Vector3.zero, Quaternion.identity);
                Runner = NewRunner.GetComponent<NetworkRunner>();
                NewRunner.transform.SetParent(Manager.gameObject.transform);
                Debug.LogWarning("Created new NetworkRunner", NewRunner);
            }
        }

        public static async Task JoinRandomRoom(string queue = null)
        {
            if (Manager.RoomData.inRoom)
                return;

            if (Runner == null)
                CreateNewRunner();

            if (queue == null)
                queue = Manager.DefaultQueue;

            Dictionary<string, SessionProperty> roomProperties = new()
            {
                { "joinable", "public" },
                { "queue", queue },
                { "version", Application.version }
            };

            StartGameResult result = await Runner.StartGame(new StartGameArgs()
            {
                GameMode = ConnectionMode,
                SessionProperties = roomProperties,
                PlayerCount = Manager.MaxPlayerCount,
                SceneManager = Manager.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                ObjectProvider = Manager.gameObject.AddComponent<NetworkObjectProviderDefault>(),
            });

            if (!result.Ok)
            {
                Debug.LogError($"Failed to join a random room! Error logged below\n {result.ShutdownReason}: {result.StackTrace}, {result.ErrorMessage}");
            }
            else
            {
                Debug.Log($"Successfully joined a random room in queue {queue}!");
            }
        }

        public static async Task JoinPrivateRoom(string room, string queue = null)
        {
            if (Manager.RoomData.inRoom)
                return;

            if (Runner == null)
                CreateNewRunner();

            if (queue == null)
                queue = Manager.DefaultQueue;

            if (room == null)
                room = GenerateNumericalCode();

            Dictionary<string, SessionProperty> roomProperties = new()
            {
                { "joinable", "private" },
                { "queue", queue },
                { "version", Application.version }
            };

            StartGameResult result = await Runner.StartGame(new StartGameArgs()
            {
                GameMode = ConnectionMode,
                SessionProperties = roomProperties,
                PlayerCount = Manager.MaxPlayerCount,
                SessionName = room,
                SceneManager = Manager.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                ObjectProvider = Manager.gameObject.AddComponent<NetworkObjectProviderDefault>(),
            });

            if (!result.Ok)
            {
                Debug.LogError($"Failed to join private room {room}! Error logged below\n {result.ShutdownReason}: {result.StackTrace}, {result.ErrorMessage}");
            }
            else
            {
                Debug.Log($"Successfully joined private room {room} in queue {queue}!");
            }
        }

        public static void LeaveRoom()
        {
            Runner.Shutdown(shutdownReason: ShutdownReason.Ok);
        }

        public struct roomData
        {
            public readonly int RoomPlayerCount { get { return Runner.SessionInfo.PlayerCount; } }
            public readonly bool InRoomAlone { get { return RoomPlayerCount == 1; } }
            public readonly bool inRoom { get { return Runner.IsCloudReady && Runner.IsConnectedToServer; } }
            public readonly List<FlowVRPlayer> SpawnedFlowVRPlayers { get { return SpawnedPlayers.Select(p => p.fusionPlayer).ToList(); } }
            public readonly List<(FlowVRPlayer fusionPlayer, PlayerRef player)> SpawnedPlayers
            {
                get
                {
                    return inRoom ? Runner.ActivePlayers
                            .Select(playerRef => (fusionPlayer: Runner.GetPlayerObject(playerRef).NetworkedBehaviours[0] as FlowVRPlayer, player: playerRef))
                            .Where(player => player.fusionPlayer != null)
                            .ToList() : new List<(FlowVRPlayer fusionPlayer, PlayerRef player)>();
                }
            }
        }

        public static string GenerateNumericalCode()
        {
            return new System.Random().Next(99999).ToString();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FlowVRManager))]
    public class FlowVRManagerGUI : Editor
    {
        private Texture2D logo;
        private const string AssetsDir = "FlowVR/Assets/";
        private const string LogoLandscapeDir = "FlowVRLandscape";

        public override void OnInspectorGUI()
        {
            if (logo == null)
            {
                logo = Resources.Load<Texture2D>($"{AssetsDir}{LogoLandscapeDir}");
            }

            GUI.DrawTexture(new Rect(60f, -40f, 460f, 190f), logo);
            EditorGUILayout.Space(100);

            base.OnInspectorGUI();

            if (GUI.Button(new Rect(70f, 195f, 200f, 25f), "Navigate to Photon Settings"))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath("Assets/Photon/Fusion/Resources/PhotonAppSettings.asset"));
            }

            if (EditorApplication.isPlaying)
            {
                if (FlowVRManager.Manager.RoomData.inRoom)
                {
                    if (GUILayout.Button("Leave Room"))
                    {
                        FlowVRManager.LeaveRoom();
                    }
                }
                else
                {
                    GUILayout.Label("Not in a room.");

                    if (GUILayout.Button("Join Random Room"))
                    {
                        FlowVRManager.JoinRandomRoom(FlowVRManager.Manager.DefaultQueue);
                    }

                    if (GUILayout.Button("Join Private Room"))
                    {
                        FlowVRManager.JoinPrivateRoom(FlowVRManager.GenerateNumericalCode(), FlowVRManager.Manager.DefaultQueue);
                    }
                }
            }
        }
    }
#endif
}