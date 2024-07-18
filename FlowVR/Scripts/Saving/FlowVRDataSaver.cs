namespace FlowVR
{
    using System.Collections.Generic;
    using UnityEngine;
    using Cosmetic = FlowVRPlayer.Cosmetic;

    /// <summary>
    /// A class that manages data saving and loading for FlowVR.
    /// </summary>
    [HelpURL("https://github.com/rxxyn/FlowVR/wiki/Player-Data-Saving")]
    public class FlowVRDataSaver
    {
        public static FlowVRPlayer Player { get { return FlowVRPlayer.Instance; } }
        public const string CosmeticLocator = "Cosmetic_";

        #region Saving

        public static void SaveUsername()
        {
            PlayerPrefs.SetString("Username", Player.username.Value);
            SaveChanges();
        }

        public static void SaveColor()
        {
            PlayerPrefs.SetString("Color", JsonUtility.ToJson(Player.color));
            SaveChanges();
        }

        public static void SaveCosmetics()
        {
            foreach (int cosmetic in Player.EquippedCosmetics)
            {
                PlayerPrefs.SetString($"{CosmeticLocator}{cosmetic}", "1");
            }
            SaveChanges();
        }

        #endregion

        #region Loading

        public static string LoadUsername()
        {
            string username = PlayerPrefs.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                return username;
            }
            return $"{FlowVRManager.Manager.DefaultName}{FlowVRManager.GenerateNumericalCode()}";
        }

        public static Color LoadColor()
        {
            string color = PlayerPrefs.GetString("Color");
            if (!string.IsNullOrEmpty(color))
            {
                return JsonUtility.FromJson<Color>(color);
            }
            Debug.LogError("Failed to load the player's color, or the player did not have a color saved.");
            return FlowVRManager.Manager.DefaultColor;
        }

        public static List<string> LoadCosmetics()
        {
            List<string> cosmetics = new();

            foreach (Cosmetic cosmetic in Player.Cosmetics)
            {
                if (!string.IsNullOrEmpty(PlayerPrefs.GetString($"{CosmeticLocator}{Player.Cosmetics.IndexOf(cosmetic)}")))
                {
                    cosmetics.Add(cosmetic.cosmeticobj.name);
                }
            }

            if (cosmetics.Count == 0)
                Debug.Log("The player had no cosmetics to load.");

            return cosmetics;
        }

        #endregion

        private static void SaveChanges() => PlayerPrefs.Save();
    }
}