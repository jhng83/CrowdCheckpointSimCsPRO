using UnityEngine;

namespace CrowdCheckpoint.Unity.Foundation
{
    /// <summary>
    /// S1 scene-wiring diagnostic only. There is no simulation, migrated UI or movement here.
    /// This explicit placeholder is removed when the application presentation is implemented.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoundationBootstrap : MonoBehaviour
    {
        public const string StageLabel = "S1 foundation only - no simulation is implemented";

        [SerializeField] private Camera foundationCamera;

        public Camera FoundationCamera { get { return foundationCamera; } }

        private void Awake()
        {
            if (foundationCamera == null)
            {
                Debug.LogError("Foundation scene is missing its serialized camera reference.", this);
                enabled = false;
                return;
            }

            Debug.Log(StageLabel, this);
        }

        private void OnGUI()
        {
            // Temporary diagnostic, not the planned uGUI application or any completed feature.
            GUI.Box(new Rect(20, 20, Mathf.Max(100, Screen.width - 40), 100), StageLabel);
            GUI.Label(new Rect(35, 55, Mathf.Max(100, Screen.width - 70), 45),
                "Scene and script wiring checkpoint. Crowd, controls, results and recording are not ported yet.");
        }
    }
}
