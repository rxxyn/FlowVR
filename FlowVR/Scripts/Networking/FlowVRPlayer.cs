namespace FlowVR
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Fusion;
    using TMPro;
    using UnityEngine;
    using System.Collections;
    using System.Linq;
    using UnityEditor;

    /// <summary>
    /// A class that manages player operations for FlowVR.
    /// </summary>
    [HelpURL("https://github.com/rxxyn/FlowVR/wiki/Player-Customization")]
    public class FlowVRPlayer : NetworkBehaviour
    {
        public static FlowVRPlayer Instance { get; private set; }
        public bool IsLocalPlayer { get { return Object.StateAuthority == FlowVRManager.Runner.LocalPlayer; } }

        [Header("Objects")]
        [SerializeField] private Transform Head;
        [SerializeField] private Transform LeftHand;
        [SerializeField] private Transform RightHand;

        public ChangeDetector changeDetector;

        [Header("Player")]
        [SerializeField] private TMP_Text usernameDisplay;

        [Networked, Capacity(20)] public NetworkString<_32> username { get; set; }
        [Networked] public Color color { get; set; }

        [SerializeField] private Renderer[] playerRenderers;
        [Networked, Capacity(10)] public NetworkLinkedList<int> EquippedCosmetics => default;
        [field: SerializeField] public List<Cosmetic> Cosmetics { get; private set; }

        [Header("Other")]
        [Tooltip("Will the name show up on the local network player?")] public bool HideLocalName = false;
        [Tooltip("Will the local network player be hid?")] public bool HideLocalPlayer = false;

        public override void Spawned()
        {
            base.Spawned();

            changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);

            if (!IsLocalPlayer) return;

            Instance = this;

            StartCoroutine(LoadPlayerData());
            StartCoroutine(SyncCosmetics());

            if (HideLocalPlayer)
            {
                foreach (Renderer pR in playerRenderers)
                {
                    pR.enabled = false;
                }
            }

            if (HideLocalName)
            {
                usernameDisplay.enabled = false;
            }
        }

        private IEnumerator SyncCosmetics()
        {
            yield return new WaitForSeconds(4f);
            if (FlowVRManager.Manager.RoomData.InRoomAlone) yield break;

            foreach (FlowVRPlayer player in FlowVRManager.Manager.RoomData.SpawnedFlowVRPlayers)
            {
                foreach (int cosmetic in player.EquippedCosmetics)
                {
                    player.Cosmetics[cosmetic].cosmeticobj.SetActive(true);
                }
            }
        }

        private IEnumerator LoadPlayerData()
        {
            yield return new WaitForSeconds(2f);
            SetUsername(FlowVRDataSaver.LoadUsername());
            SetColor(FlowVRDataSaver.LoadColor());
            LoadCosmetics();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (IsLocalPlayer)
            {
                Head.transform.position = FlowVRManager.Manager.Head.transform.position;
                Head.transform.rotation = FlowVRManager.Manager.Head.transform.rotation;

                LeftHand.transform.position = FlowVRManager.Manager.LeftHand.transform.position;
                LeftHand.transform.rotation = FlowVRManager.Manager.LeftHand.transform.rotation;

                RightHand.transform.position = FlowVRManager.Manager.RightHand.transform.position;
                RightHand.transform.rotation = FlowVRManager.Manager.RightHand.transform.rotation;
            }
        }

        #region Player Customization

        /// <summary>
        /// Sets the Player's username.
        /// </summary>
        /// <param name="newUsername">The new username of the player. Max length is 20. Cannot be null or be the same value as the current Player's username.</param>
        public void SetUsername(string newUsername)
        {
            if (!IsLocalPlayer) return;
            if (newUsername != null && newUsername != this.username)
            {
                this.username = newUsername;
                FlowVRDataSaver.SaveUsername();
            }
        }

        /// <summary>
        /// Sets the Player's color.
        /// </summary>
        /// <param name="color">The new color of the player. Cannot be the same value as the current Player's color.</param>
        public void SetColor(Color color)
        {
            if (!IsLocalPlayer) return;
            if (color != this.color)
            {
                this.color = color;
                FlowVRDataSaver.SaveColor();
            }
        }

        /// <summary>
        /// Sets a cosmetic on the Player.
        /// </summary>
        /// <param name="cosmetic">The name of the cosmetic. This must be the same name as the GameObject set on the Cosmetic item struct.</param>
        /// <param name="equiped">Enable or disable the cosmetic given.</param>
        public void SetCosmetic(string cosmetic, bool equiped)
        {
            if (!IsLocalPlayer) return;
            if (cosmetic != null && Cosmetics.Exists(c => c.cosmeticobj.name == cosmetic))
            {
                // FlowVR stops syncing cosmetics over the network after 10 are enabled.
                if (EquippedCosmetics.Count == 10)
                    return;

                // You could implement a inventory check here with PlayFab to make sure people can't equip cosmetics they dont own

                RPCSetCosmetic(Encoding.UTF8.GetBytes(cosmetic), Convert.ToByte(equiped));
                ModifyEquippedCosmetics(cosmetic, equiped);
            }
            else
            {
                Debug.LogError("The cosmetic that was passed was null, or does NOT exist in the Cosmetics struct on the player, or the cosmetic name given does NOT match a cosmetic GameObject's name on the Cosmetics struct on the player.");
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPCSetCosmetic(byte[] cosmetic, byte equiped, RpcInfo info = default)
        {
            Cosmetics.Find(c => c.cosmeticobj.name == Encoding.UTF8.GetString(cosmetic)).Set(Convert.ToBoolean(equiped));
        }

        private void ModifyEquippedCosmetics(string cosmetic, bool choice)
        {
            if (!IsLocalPlayer) return;

            if (choice)
            {
                if (!EquippedCosmetics.Contains(Cosmetics.FindIndex(c => c.cosmeticobj.name == cosmetic)))
                    EquippedCosmetics.Add(Cosmetics.FindIndex(c => c.cosmeticobj.name == cosmetic));
            }
            else
            {
                if (EquippedCosmetics.Contains(Cosmetics.FindIndex(c => c.cosmeticobj.name == cosmetic)))
                    EquippedCosmetics.Remove(Cosmetics.FindIndex(c => c.cosmeticobj.name == cosmetic));
            }

            FlowVRDataSaver.SaveCosmetics();
        }

        private void LoadCosmetics()
        {
            List<string> loadedCosmetics = FlowVRDataSaver.LoadCosmetics();
            foreach (string cosmetic in loadedCosmetics)
            {
                SetCosmetic(cosmetic, true);
            }
        }

        #endregion

        #region Networked Properties
        public override void Render()
        {
            base.Render();

            foreach (var change in changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(username):
                        OnUsernameChanged();
                        break;
                    case nameof(color):
                        OnColorChanged();
                        break;
                }
            }
        }

        public void OnUsernameChanged()
        {
            usernameDisplay.color = Color.white;
            usernameDisplay.text = username.Value;
            gameObject.name = username.Value;
        }

        public void OnColorChanged()
        {
            foreach (Renderer x in playerRenderers)
                x.material.color = color;
        }
        #endregion

        [Serializable]
        public struct Cosmetic
        {
            [Tooltip("The name of this GameObject is the ID/Name of the cosmetic.")]
            [field: SerializeField] public GameObject cosmeticobj { get; private set; }

            public void Set(bool enable)
            {
                if (this.cosmeticobj != null)
                    this.cosmeticobj.SetActive(enable);
            }
        }
    }
}