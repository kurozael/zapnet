using Dissonance.Editor;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Integrations.Zapnet.Editor
{
    [CustomEditor(typeof(ZapnetCommsNetwork))]
    public class ZapnetCommsNetworkEditor
        : BaseDissonnanceCommsNetworkEditor<ZapnetCommsNetwork, ZapnetServer, ZapnetClient, ZapnetPeer, Unit, Unit>
    {
        private bool _advanced;

        private SerializedProperty _voiceDataChannelToServer;
        private SerializedProperty _systemMessagesChannelToServer;
        private SerializedProperty _voiceDataChannelToClient;
        private SerializedProperty _systemMessagesChannelToClient;

        protected void OnEnable()
        {
            _voiceDataChannelToServer = serializedObject.FindProperty("_voiceDataChannelToServer");
            _systemMessagesChannelToServer = serializedObject.FindProperty("_systemMessagesChannelToServer");
            _voiceDataChannelToClient = serializedObject.FindProperty("_voiceDataChannelToClient");
            _systemMessagesChannelToClient = serializedObject.FindProperty("_systemMessagesChannelToClient");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(_voiceDataChannelToServer);
                EditorGUILayout.PropertyField(_systemMessagesChannelToServer);
                EditorGUILayout.PropertyField(_voiceDataChannelToClient);
                EditorGUILayout.PropertyField(_systemMessagesChannelToClient);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}