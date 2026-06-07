#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StoryEvents.Editor
{
    /// <summary>
    /// Custom inspector for EventChainSO that draws a visual preview
    /// of the chain's node graph directly in the Inspector.
    /// Makes it easier for designers to see the chain structure at a glance.
    /// </summary>
    [CustomEditor(typeof(EventChainSO))]
    public class EventChainSOEditor : UnityEditor.Editor
    {
        private bool _showChainPreview = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            _showChainPreview = EditorGUILayout.Foldout(_showChainPreview, "Chain Preview", true, EditorStyles.foldoutHeader);

            if (!_showChainPreview) return;

            var chain = (EventChainSO)target;
            if (chain.entryNode == null)
            {
                EditorGUILayout.HelpBox("Assign an Entry Node to preview the chain.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            DrawChainPreview(chain.entryNode, 0, new System.Collections.Generic.HashSet<EventNodeSO>());
        }

        private void DrawChainPreview(EventNodeSO node, int depth, System.Collections.Generic.HashSet<EventNodeSO> visited)
        {
            if (node == null || visited.Contains(node)) return;
            visited.Add(node);

            // Indent by depth
            EditorGUI.indentLevel = depth;

            // Color code by type
            Color prevColor = GUI.backgroundColor;
            if (node.isEndNode)
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // green = end
            else if (node.HasChoices)
                GUI.backgroundColor = new Color(1f, 0.9f, 0.5f); // amber = choice
            else
                GUI.backgroundColor = new Color(0.85f, 0.85f, 0.95f); // blue-gray = continue

            string icon = node.isEndNode ? "⬛" : node.HasChoices ? "⬦" : "▸";
            string label = $"{icon} {(string.IsNullOrEmpty(node.title) ? node.name : node.title)}";
            if (node.isEndNode && node.outcome != null)
                label += $"  [{node.outcome.name}]";

            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Height(22)))
            {
                Selection.activeObject = node;
                EditorGUIUtility.PingObject(node);
            }

            GUI.backgroundColor = prevColor;

            // Recurse
            if (node.HasChoices)
            {
                foreach (var choice in node.choices)
                {
                    EditorGUI.indentLevel = depth + 1;
                    EditorGUILayout.LabelField($"  → \"{choice?.label}\"", EditorStyles.miniLabel);
                    if (choice?.nextNode != null)
                        DrawChainPreview(choice.nextNode, depth + 2, visited);
                }
            }
            else if (node.continueNode != null)
            {
                DrawChainPreview(node.continueNode, depth + 1, visited);
            }

            EditorGUI.indentLevel = 0;
        }
    }

    // ── EventNodeSO custom inspector ────────────────────────────────────────

    [CustomEditor(typeof(EventNodeSO))]
    public class EventNodeSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var node = (EventNodeSO)target;

            EditorGUILayout.Space(6);

            if (node.HasChoices && node.continueNode != null)
            {
                EditorGUILayout.HelpBox(
                    "This node has both Choices and a Continue Node. The Continue Node is ignored when Choices are present.",
                    MessageType.Warning);
            }

            if (node.isEndNode && node.continueNode != null)
            {
                EditorGUILayout.HelpBox(
                    "This node is marked as End Node but also has a Continue Node set. The Continue Node will never be reached.",
                    MessageType.Warning);
            }

            if (!node.isEndNode && !node.HasChoices && node.continueNode == null)
            {
                EditorGUILayout.HelpBox(
                    "This node has no Choices and no Continue Node. It will silently end the chain.",
                    MessageType.Warning);
            }

            if (node.isEndNode)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("End Node", EditorStyles.boldLabel);

                if (node.outcome == null)
                    EditorGUILayout.HelpBox("No outcome assigned. Chain will end with no game effect.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox($"Outcome: {node.outcome.name}\n{node.outcome.GetPreviewText() ?? "(no preview)"}", MessageType.None);
            }
        }
    }
}
#endif
