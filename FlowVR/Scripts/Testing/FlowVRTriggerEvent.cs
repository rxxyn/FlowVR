namespace FlowVR
{
    using UnityEngine;

    public class FlowVRTriggerEvent : MonoBehaviour
    {
        public string stringValue;
        public Color colorValue;
        public bool boolValue;
        public triggerEvent TriggerEvent;

#if UNITY_EDITOR
        public bool UnityClick;

        private void Update()
        {
            if (UnityClick)
            {
                OnTriggerEnter(null);
                UnityClick = false;
            }
        }
#endif

        public FlowVRPlayer player { get { return FlowVRPlayer.Instance; } }

        public void OnTriggerEnter(Collider other)
        {
            switch (TriggerEvent)
            {
                case triggerEvent.SetUsername:
                    player.SetUsername(stringValue);
                    break;
                case triggerEvent.SetColor:
                    player.SetColor(colorValue);
                    break;
                case triggerEvent.SetCosmetic:
                    player.SetCosmetic(stringValue, boolValue);
                    break;
            }
        }

        public enum triggerEvent
        {
            SetUsername,
            SetColor,
            SetCosmetic,
        }
    }
}