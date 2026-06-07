#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StoryEvents.Outcomes;
using StoryEvents;

namespace CK2Events.Editor
{
    // ════════════════════════════════════════════════════════════════════════════
    //  EventChainEditorWindow
    //  Open via:  Window → CK2 Events → Chain Editor
    // ════════════════════════════════════════════════════════════════════════════
    public class EventChainEditorWindow : EditorWindow
    {
        // ── Layout constants ────────────────────────────────────────────────
        private const float SidebarW    = 220f;
        private const float InspectorW  = 360f;   // wider panel
        private const float TopbarH     = 36f;
        private const float NodeW       = 200f;
        private const float NodeH       = 80f;
        private const float ChoicePortH = 20f;
        private const float PortRadius  = 6f;
        private const float GridSize    = 20f;

        // ── Zoom ────────────────────────────────────────────────────────────
        private float _zoom        = 1f;
        private const float ZoomMin = 0.25f;
        private const float ZoomMax = 2.5f;

        // ── Node colors ─────────────────────────────────────────────────────
        private static readonly Color ColContinue = new(0.25f, 0.35f, 0.55f);
        private static readonly Color ColChoice   = new(0.55f, 0.40f, 0.10f);
        private static readonly Color ColEnd      = new(0.15f, 0.50f, 0.25f);
        private static readonly Color ColSelected = new(0.85f, 0.70f, 0.15f);
        private static readonly Color ColBg       = new(0.14f, 0.14f, 0.14f);
        private static readonly Color ColGrid     = new(0.18f, 0.18f, 0.18f);
        private static readonly Color ColGridBold = new(0.22f, 0.22f, 0.22f);
        private static readonly Color ColCurve    = new(0.70f, 0.70f, 0.70f);
        private static readonly Color ColCurveChoice = new(0.90f, 0.75f, 0.30f);
        private static readonly Color ColSidebar  = new(0.17f, 0.17f, 0.17f);
        private static readonly Color ColInspBg   = new(0.18f, 0.18f, 0.18f);
        private static readonly Color ColHeader   = new(0.12f, 0.12f, 0.12f);

        // ── State ────────────────────────────────────────────────────────────
        private EventChainSO          _activeChain;
        private EventNodeSO           _selectedNode;
        private int                   _selectedChoiceIdx = -1;   // within selected node

        // Graph layout: node → canvas position (persisted via EditorPrefs as JSON)
        private readonly Dictionary<EventNodeSO, Vector2> _nodePositions = new();
        private readonly HashSet<EventNodeSO>             _allNodes      = new();

        // Interaction
        private Vector2 _graphOffset   = Vector2.zero;
        private Vector2 _dragStart;
        private bool    _isPanningGraph;
        private EventNodeSO _draggingNode;
        private Vector2     _draggingNodeOffset;

        // Connection drawing: set when user starts dragging from an output port
        private bool        _drawingConnection;
        private EventNodeSO _connFromNode;
        private int         _connFromChoiceIdx; // -1 = continue port

        // Sidebar scroll
        private Vector2 _sidebarScroll;
        // Inspector scroll
        private Vector2 _inspectorScroll;

        // New-chain popup
        private bool   _showNewChainPopup;
        private string _newChainName = "New Chain";

        // New-node position for next creation
        private Vector2 _nextNodePos = new(40, 40);

        // Cached chain list
        private List<EventChainSO> _allChains = new();
        private double             _lastChainRefresh;

        // ── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("Window/CK2 Events/Chain Editor")]
        public static void Open()
        {
            var win = GetWindow<EventChainEditorWindow>("CK2 Chain Editor");
            win.minSize = new Vector2(1000, 580);
        }

        // ── Unity callbacks ──────────────────────────────────────────────────
        private void OnEnable()
        {
            RefreshChainList();
            titleContent = new GUIContent("CK2 Chain Editor", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
        }

        private void OnGUI()
        {
            // Refresh chain list every second
            if (EditorApplication.timeSinceStartup - _lastChainRefresh > 1.0)
                RefreshChainList();

            Rect full = new(0, 0, position.width, position.height);

            // ── Top bar ──
            DrawTopbar(new Rect(0, 0, position.width, TopbarH));

            // ── Sidebar ──
            Rect sidebarRect = new(0, TopbarH, SidebarW, position.height - TopbarH);
            DrawSidebar(sidebarRect);

            // ── Inspector ──
            Rect inspectorRect = new(position.width - InspectorW, TopbarH, InspectorW, position.height - TopbarH);
            DrawInspector(inspectorRect);

            // ── Graph ──
            float graphX = SidebarW;
            float graphW = position.width - SidebarW - InspectorW;
            Rect graphRect = new(graphX, TopbarH, graphW, position.height - TopbarH);
            DrawGraph(graphRect);

            // ── New chain modal ──
            if (_showNewChainPopup)
                DrawNewChainModal();

            // Repaint while connection is being drawn
            if (_drawingConnection)
                Repaint();
        }

        // ════════════════════════════════════════════════════════════════════
        //  TOP BAR
        // ════════════════════════════════════════════════════════════════════
        private void DrawTopbar(Rect rect)
        {
            EditorGUI.DrawRect(rect, ColHeader);

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            GUILayout.Label("CK2 Event Chain Editor", EditorStyles.boldLabel, GUILayout.Width(200));

            GUILayout.Space(8);

            if (_activeChain != null)
            {
                GUILayout.Label($"Editing:  {_activeChain.name}", EditorStyles.label);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Auto Layout", GUILayout.Width(90), GUILayout.Height(22)))
                    AutoLayoutNodes();

                if (GUILayout.Button("Frame All", GUILayout.Width(80), GUILayout.Height(22)))
                    FrameAllNodes();

                GUILayout.Space(6);
                GUILayout.Label($"Zoom: {_zoom * 100f:0}%",
                    new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter },
                    GUILayout.Width(72), GUILayout.Height(22));
                if (GUILayout.Button("1:1", GUILayout.Width(30), GUILayout.Height(22)))
                {
                    _zoom = 1f;
                    Repaint();
                }

                GUILayout.Space(8);

                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.4f);
                if (GUILayout.Button("+ Node", GUILayout.Width(70), GUILayout.Height(22)))
                    CreateNode();
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("Select or create a chain on the left.", MessageType.None);
            }

            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ════════════════════════════════════════════════════════════════════
        //  SIDEBAR — chain list
        // ════════════════════════════════════════════════════════════════════
        private void DrawSidebar(Rect rect)
        {
            EditorGUI.DrawRect(rect, ColSidebar);

            // Header
            Rect headerRect = new(rect.x, rect.y, rect.width, 28);
            EditorGUI.DrawRect(headerRect, ColHeader);
            GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 60, 18), "Event Chains", EditorStyles.boldLabel);

            // New chain button
            if (GUI.Button(new Rect(rect.xMax - 52, rect.y + 4, 46, 20), "+ New"))
            {
                _showNewChainPopup = true;
                _newChainName = "NewChain";
            }

            // Chain list
            Rect listRect = new(rect.x, rect.y + 28, rect.width, rect.height - 28);
            GUILayout.BeginArea(listRect);
            _sidebarScroll = GUILayout.BeginScrollView(_sidebarScroll);

            foreach (var chain in _allChains)
            {
                if (chain == null) continue;
                bool isActive = chain == _activeChain;

                GUI.backgroundColor = isActive ? new Color(0.25f, 0.45f, 0.7f) : new Color(0.22f, 0.22f, 0.22f);
                if (GUILayout.Button(chain.name, GUILayout.Height(28)))
                {
                    LoadChain(chain);
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ════════════════════════════════════════════════════════════════════
        //  INSPECTOR — right panel
        // ════════════════════════════════════════════════════════════════════
        private void DrawInspector(Rect rect)
        {
            EditorGUI.DrawRect(rect, ColInspBg);

            // Header
            Rect headerRect = new(rect.x, rect.y, rect.width, 32);
            EditorGUI.DrawRect(headerRect, ColHeader);

            string headerLabel = _selectedNode != null ? "Node Inspector"
                               : _activeChain  != null ? "Chain Inspector"
                                                       : "Inspector";
            GUI.Label(new Rect(rect.x + 10, rect.y + 7, rect.width - 16, 20), headerLabel,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });

            Rect scrollRect = new(rect.x, rect.y + 32, rect.width, rect.height - 32);
            GUILayout.BeginArea(scrollRect);
            _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll);

            if (_selectedNode != null)
                DrawNodeInspector(_selectedNode);
            else if (_activeChain != null)
                DrawChainInspector(_activeChain);
            else
                GUILayout.Label("Nothing selected.", EditorStyles.centeredGreyMiniLabel);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawChainInspector(EventChainSO chain)
        {
            SerializedObject so = new(chain);
            so.Update();

            GUILayout.Space(6);
            InspSection("Identity");
            GUILayout.Space(4);
            InspField(so, "chainId",      "Chain ID");
            GUILayout.Space(4);
            InspField(so, "defaultTitle", "Default Title");

            GUILayout.Space(4);
            InspSection("Selection");
            GUILayout.Space(4);
            InspField(so, "weight",       "Weight");
            GUILayout.Space(4);
            InspField(so, "isRepeatable", "Repeatable");

            GUILayout.Space(4);
            InspSection("Requirements");
            GUILayout.Space(4);
            InspField(so, "requiredCompletedChainIds", "Required Chain IDs");

            so.ApplyModifiedProperties();

            GUILayout.Space(12);
            if (GUILayout.Button("Ping Asset in Project", GUILayout.Height(26)))
                EditorGUIUtility.PingObject(chain);
            GUILayout.Space(20);
        }

        private void DrawNodeInspector(EventNodeSO node)
        {
            SerializedObject so = new(node);
            so.Update();

            GUILayout.Space(6);

            // ── Display ──
            InspSection("Display");
            GUILayout.Space(4);
            InspField(so, "artwork", "Artwork");
            GUILayout.Space(6);

            GUILayout.Label("Title", EditorStyles.boldLabel);
            SerializedProperty titleProp = so.FindProperty("title");
            titleProp.stringValue = EditorGUILayout.TextField(titleProp.stringValue, GUILayout.Height(22));
            GUILayout.Space(6);

            GUILayout.Label("Body Text", EditorStyles.boldLabel);
            SerializedProperty bodyProp = so.FindProperty("bodyText");
            bodyProp.stringValue = EditorGUILayout.TextArea(bodyProp.stringValue,
                new GUIStyle(EditorStyles.textArea) { wordWrap = true },
                GUILayout.MinHeight(90), GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            // ── Node type ──
            InspSection("Node Type");
            GUILayout.Space(4);
            SerializedProperty isEnd = so.FindProperty("isEndNode");
            EditorGUILayout.PropertyField(isEnd, new GUIContent("Is End Node"));
            GUILayout.Space(2);

            if (!isEnd.boolValue)
            {
                SerializedProperty hasCh = so.FindProperty("choices");
                bool hasChoices = hasCh.arraySize > 0;

                if (!hasChoices)
                {
                    GUILayout.Space(6);
                    GUILayout.Label("Continue Path", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic });
                    InspField(so, "continueNode", "Next Node");
                }
            }

            // ── Choices ──
            if (!isEnd.boolValue)
            {
                InspSection("Choices");
                GUILayout.Space(4);
                SerializedProperty choicesArr = so.FindProperty("choices");

                for (int i = 0; i < choicesArr.arraySize; i++)
                {
                    SerializedProperty choiceProp = choicesArr.GetArrayElementAtIndex(i);
                    ChoiceSO choice = choiceProp.objectReferenceValue as ChoiceSO;

                    bool isSelectedChoice = _selectedChoiceIdx == i;
                    GUI.backgroundColor = isSelectedChoice ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.22f, 0.22f, 0.22f);

                    GUILayout.BeginVertical("box");
                    GUI.backgroundColor = Color.white;

                    GUILayout.Space(2);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Choice {i + 1}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
                    GUILayout.FlexibleSpace();
                    GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                    if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        GUI.backgroundColor = Color.white;
                        DeleteChoice(node, i);
                        so.ApplyModifiedProperties();
                        return;
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);

                    if (choice != null)
                    {
                        SerializedObject cso = new(choice);
                        cso.Update();

                        GUILayout.Label("Label", EditorStyles.miniLabel);
                        SerializedProperty labelProp = cso.FindProperty("label");
                        labelProp.stringValue = EditorGUILayout.TextField(labelProp.stringValue, GUILayout.Height(22));
                        GUILayout.Space(4);

                        EditorGUILayout.PropertyField(cso.FindProperty("nextNode"), new GUIContent("Next Node"));
                        GUILayout.Space(2);
                        EditorGUILayout.PropertyField(cso.FindProperty("immediateOutcome"), new GUIContent("Immediate Outcome"));

                        cso.ApplyModifiedProperties();
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(choiceProp, new GUIContent("Choice Asset"));
                    }

                    GUILayout.Space(4);
                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }

                GUILayout.Space(2);
                GUI.backgroundColor = new Color(0.20f, 0.50f, 0.30f);
                if (GUILayout.Button("+ Add Choice", GUILayout.Height(28)))
                    AddChoiceToNode(node);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(4);
            }

            // ── End node outcome ──
            if (isEnd.boolValue)
            {
                InspSection("Outcome");
                GUILayout.Space(4);
                InspField(so, "outcome", "Outcome Asset");

                EventOutcomeSO outcome = so.FindProperty("outcome").objectReferenceValue as EventOutcomeSO;
                if (outcome != null)
                {
                    string preview = outcome.GetPreviewText();
                    if (preview != null)
                    {
                        GUILayout.Space(4);
                        GUI.color = new Color(0.6f, 1f, 0.6f);
                        GUILayout.Label($"  ⇒  {preview}",
                            new GUIStyle(EditorStyles.label) { fontSize = 12 });
                        GUI.color = Color.white;
                    }
                }

                GUILayout.Space(8);
                GUILayout.Label("Quick Create", EditorStyles.boldLabel);
                GUILayout.Space(2);

                if (GUILayout.Button("Create Stat Change Outcome", GUILayout.Height(26)))
                    CreateAndAssignOutcome<StatChangeOutcome>(node, "StatChange");
                GUILayout.Space(2);
                if (GUILayout.Button("Create Set Flag Outcome", GUILayout.Height(26)))
                    CreateAndAssignOutcome<SetFlagOutcome>(node, "SetFlag");
                GUILayout.Space(2);
                if (GUILayout.Button("Create Compound Outcome", GUILayout.Height(26)))
                    CreateAndAssignOutcome<CompoundOutcome>(node, "Compound");
            }

            so.ApplyModifiedProperties();

            GUILayout.Space(16);

            // ── Actions ──
            InspSection("Actions");
            GUILayout.Space(4);
            if (GUILayout.Button("Ping Asset in Project", GUILayout.Height(26)))
                EditorGUIUtility.PingObject(node);
            GUILayout.Space(4);

            GUI.backgroundColor = new Color(0.7f, 0.25f, 0.25f);
            if (GUILayout.Button("Delete Node", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Delete Node",
                    $"Delete '{node.name}' and remove all references to it in this chain?", "Delete", "Cancel"))
                    DeleteNode(node);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.Space(20);
        }

        // ════════════════════════════════════════════════════════════════════
        //  GRAPH CANVAS
        // ════════════════════════════════════════════════════════════════════
        private void DrawGraph(Rect rect)
        {
            GUI.BeginClip(rect);
            Rect local = new(0, 0, rect.width, rect.height);

            // Background
            EditorGUI.DrawRect(local, ColBg);
            DrawGrid(local);

            if (_activeChain == null)
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(new Rect(local.width / 2 - 120, local.height / 2 - 10, 240, 24),
                    "Select or create a chain to begin.", EditorStyles.centeredGreyMiniLabel);
                GUI.color = Color.white;
                GUI.EndClip();
                return;
            }

            // Draw curves first (behind nodes)
            DrawConnections();

            // Connection being dragged
            if (_drawingConnection)
            {
                Vector2 from = GetOutputPortPos(_connFromNode, _connFromChoiceIdx);
                Vector2 to   = Event.current.mousePosition;
                DrawCurve(from, to, ColCurveChoice);
            }

            // Draw nodes
            foreach (var node in _allNodes.ToList())
            {
                if (node == null) continue;
                DrawNode(node);
            }

            HandleGraphEvents(local, rect);

            GUI.EndClip();
        }

        private void DrawGrid(Rect rect)
        {
            int stepSmall = (int)GridSize;
            int stepBig   = stepSmall * 5;

            int offsetX = (int)(_graphOffset.x % stepSmall);
            int offsetY = (int)(_graphOffset.y % stepSmall);

            int countX = Mathf.CeilToInt(rect.width  / stepSmall) + 1;
            int countY = Mathf.CeilToInt(rect.height / stepSmall) + 1;

            Handles.BeginGUI();
            for (int i = 0; i <= countX; i++)
            {
                float x = offsetX + i * stepSmall;
                bool bold = Mathf.RoundToInt((x - offsetX) / stepSmall) % 5 == 0;
                Handles.color = bold ? ColGridBold : ColGrid;
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, rect.height));
            }
            for (int i = 0; i <= countY; i++)
            {
                float y = offsetY + i * stepSmall;
                bool bold = Mathf.RoundToInt((y - offsetY) / stepSmall) % 5 == 0;
                Handles.color = bold ? ColGridBold : ColGrid;
                Handles.DrawLine(new Vector3(0, y), new Vector3(rect.width, y));
            }
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawConnections()
        {
            Handles.BeginGUI();
            foreach (var node in _allNodes)
            {
                if (node == null) continue;

                if (node.HasChoices)
                {
                    for (int i = 0; i < node.choices.Length; i++)
                    {
                        var choice = node.choices[i];
                        if (choice?.nextNode == null || !_allNodes.Contains(choice.nextNode)) continue;

                        Vector2 from = GetOutputPortPos(node, i);
                        Vector2 to   = GetInputPortPos(choice.nextNode);
                        DrawCurve(from, to, ColCurveChoice);
                    }
                }
                else if (node.continueNode != null && _allNodes.Contains(node.continueNode))
                {
                    Vector2 from = GetOutputPortPos(node, -1);
                    Vector2 to   = GetInputPortPos(node.continueNode);
                    DrawCurve(from, to, ColCurve);
                }
            }
            Handles.EndGUI();
        }

        private void DrawCurve(Vector2 from, Vector2 to, Color col)
        {
            float dist   = Mathf.Abs(to.x - from.x) * 0.5f + 40f;
            Vector3 tan1 = new(from.x + dist, from.y, 0);
            Vector3 tan2 = new(to.x   - dist, to.y,   0);
            Handles.DrawBezier(from, to, tan1, tan2, col, null, 2f);

            // Arrow
            Vector2 dir    = (to - from).normalized;
            Vector2 arrTip = to;
            Vector2 arrL   = arrTip - dir * 10 + new Vector2(-dir.y, dir.x) * 5;
            Vector2 arrR   = arrTip - dir * 10 - new Vector2(-dir.y, dir.x) * 5;
            Handles.color = col;
            Handles.DrawLine(arrTip, arrL);
            Handles.DrawLine(arrTip, arrR);
        }

        private void DrawNode(EventNodeSO node)
        {
            Vector2 screenPos = GraphToScreen(GetNodePos(node));
            float   h         = GetNodeHeight(node);
            Rect    nodeRect  = new(screenPos.x, screenPos.y, NodeW * _zoom, h * _zoom);

            bool isSelected = node == _selectedNode;

            // Shadow
            EditorGUI.DrawRect(new Rect(nodeRect.x + 3, nodeRect.y + 3, nodeRect.width, nodeRect.height),
                new Color(0, 0, 0, 0.4f));

            // Node body color
            Color bodyCol = node.isEndNode ? ColEnd
                          : node.HasChoices ? ColChoice
                          : ColContinue;
            EditorGUI.DrawRect(nodeRect, bodyCol);

            // Selection outline
            if (isSelected)
            {
                Handles.BeginGUI();
                Handles.color = ColSelected;
                float t = 2f;
                Handles.DrawLine(new Vector3(nodeRect.xMin - t, nodeRect.yMin - t), new Vector3(nodeRect.xMax + t, nodeRect.yMin - t));
                Handles.DrawLine(new Vector3(nodeRect.xMax + t, nodeRect.yMin - t), new Vector3(nodeRect.xMax + t, nodeRect.yMax + t));
                Handles.DrawLine(new Vector3(nodeRect.xMax + t, nodeRect.yMax + t), new Vector3(nodeRect.xMin - t, nodeRect.yMax + t));
                Handles.DrawLine(new Vector3(nodeRect.xMin - t, nodeRect.yMax + t), new Vector3(nodeRect.xMin - t, nodeRect.yMin - t));
                Handles.color = Color.white;
                Handles.EndGUI();
            }

            // Push zoom matrix for text/labels inside the node
            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * _zoom, screenPos);

            // Header bar (in unscaled node space — GUI.matrix handles scaling)
            Rect headerRect = new(screenPos.x, screenPos.y, NodeW, 24);
            EditorGUI.DrawRect(headerRect, new Color(0, 0, 0, 0.35f));

            // Type badge
            string typeLabel = node.isEndNode ? "END" : node.HasChoices ? "CHOICE" : "CONTINUE";
            GUI.color = new Color(1, 1, 1, 0.7f);
            GUI.Label(new Rect(screenPos.x + 4, screenPos.y + 4, 60, 16), typeLabel,
                new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, fontStyle = FontStyle.Bold });
            GUI.color = Color.white;

            // Node name
            string displayName = string.IsNullOrEmpty(node.title) ? node.name : node.title;
            if (displayName.Length > 22) displayName = displayName[..22] + "…";
            GUI.Label(new Rect(screenPos.x + 4, screenPos.y + 26, NodeW - 8, 18),
                displayName, EditorStyles.boldLabel);

            // Body preview
            if (!string.IsNullOrEmpty(node.bodyText))
            {
                string preview = node.bodyText.Length > 60 ? node.bodyText[..60] + "…" : node.bodyText;
                GUI.color = new Color(1, 1, 1, 0.65f);
                GUI.Label(new Rect(screenPos.x + 4, screenPos.y + 44, NodeW - 8, 30),
                    preview, new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
                GUI.color = Color.white;
            }

            // Choice labels
            if (node.HasChoices)
            {
                for (int i = 0; i < node.choices.Length; i++)
                {
                    Vector2 portScreen = GetOutputPortPos(node, i);
                    // Convert back to unscaled for label (matrix already applied)
                    Vector2 portUnscaled = (portScreen - screenPos) / _zoom + screenPos;
                    string choiceLabel = node.choices[i]?.label ?? "?";
                    if (choiceLabel.Length > 18) choiceLabel = choiceLabel[..18] + "…";
                    GUI.color = new Color(1, 1, 1, 0.75f);
                    float ly = portUnscaled.y - 8;
                    GUI.Label(new Rect(screenPos.x + NodeW - 134, ly, 120, 16), choiceLabel,
                        new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });
                    GUI.color = Color.white;
                }
            }

            // Outcome badge
            if (node.isEndNode && node.outcome != null)
            {
                string outcomeText = node.outcome.GetPreviewText() ?? node.outcome.name;
                GUI.color = new Color(0.6f, 1f, 0.6f, 0.9f);
                GUI.Label(new Rect(screenPos.x + 4, screenPos.y + h - 20, NodeW - 8, 18),
                    $"⇒ {outcomeText}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            GUI.matrix = oldMatrix;

            // Ports drawn after matrix reset (already in screen space)
            DrawPort(GetInputPortPos(node), false);
            if (node.HasChoices)
                for (int i = 0; i < node.choices.Length; i++)
                    DrawPort(GetOutputPortPos(node, i), true);
            else if (!node.isEndNode)
                DrawPort(GetOutputPortPos(node, -1), true);
        }

        private void DrawPort(Vector2 pos, bool isOutput)
        {
            Rect portRect = new(pos.x - PortRadius, pos.y - PortRadius, PortRadius * 2, PortRadius * 2);
            EditorGUI.DrawRect(portRect, isOutput ? new Color(0.8f, 0.8f, 0.2f) : new Color(0.3f, 0.7f, 0.9f));
        }

        // ════════════════════════════════════════════════════════════════════
        //  GRAPH EVENT HANDLING
        // ════════════════════════════════════════════════════════════════════
        private void HandleGraphEvents(Rect local, Rect globalRect)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition; // in graph-local space (already clipped)

            // ContextMenu handled separately — mixing plain and 'when' cases
            // in the same switch causes a compiler error in older Unity toolchains.
            

            if (e.type == EventType.ScrollWheel)
            {
                float oldZoom   = _zoom;
                float zoomDelta = -e.delta.y * 0.05f;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, ZoomMin, ZoomMax);

                // Keep the point under the cursor fixed while zooming
                Vector2 mouseWorld = (mousePos - _graphOffset) / oldZoom;
                _graphOffset = mousePos - mouseWorld * _zoom;

                Repaint();
                e.Use();
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0:
                    HandleLeftMouseDown(mousePos);
                    break;

                case EventType.MouseDown when e.button == 2:
                case EventType.MouseDown when e.button == 1 && !HitAnyNode(mousePos):
                    _isPanningGraph = true;
                    _dragStart = mousePos;
                    e.Use();
                    break;

                case EventType.MouseDrag when e.button == 0:
                    if (_draggingNode != null)
                    {
                        _nodePositions[_draggingNode] = ScreenToGraph(mousePos + _draggingNodeOffset);
                        SaveNodePositions();
                        Repaint();
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag when _isPanningGraph:
                    _graphOffset += mousePos - _dragStart;
                    _dragStart = mousePos;
                    Repaint();
                    e.Use();
                    break;

                case EventType.MouseUp when e.button == 0:
                    if (_drawingConnection)
                    {
                        EventNodeSO targetNode = GetNodeAtInputPort(mousePos);
                        if (targetNode != null && targetNode != _connFromNode)
                            FinishConnection(targetNode);

                        _drawingConnection = false;
                        _connFromNode      = null;
                        Repaint();
                        e.Use();
                    }
                    _draggingNode   = null;
                    _isPanningGraph = false;
                    break;

                case EventType.MouseUp when e.button == 1 || e.button == 2:
                    _isPanningGraph = false;
                    break;
            }
        }

        private void HandleLeftMouseDown(Vector2 mousePos)
        {
            Event e = Event.current;

            // 1. Check output ports first (start connection drag)
            foreach (var node in _allNodes.ToList())
            {
                if (node == null) continue;

                if (node.HasChoices)
                {
                    for (int i = 0; i < node.choices.Length; i++)
                    {
                        if (HitPort(mousePos, GetOutputPortPos(node, i)))
                        {
                            _drawingConnection  = true;
                            _connFromNode       = node;
                            _connFromChoiceIdx  = i;
                            e.Use();
                            return;
                        }
                    }
                }
                else if (!node.isEndNode)
                {
                    if (HitPort(mousePos, GetOutputPortPos(node, -1)))
                    {
                        _drawingConnection  = true;
                        _connFromNode       = node;
                        _connFromChoiceIdx  = -1;
                        e.Use();
                        return;
                    }
                }
            }

            // 2. Check node body (select + start drag)
            foreach (var node in _allNodes.ToList().AsEnumerable().Reverse())
            {
                if (node == null) continue;
                Vector2 nodeScreenPos = GraphToScreen(GetNodePos(node));
                Rect nodeRect = new(nodeScreenPos.x, nodeScreenPos.y, NodeW, GetNodeHeight(node));

                if (nodeRect.Contains(mousePos))
                {
                    _selectedNode      = node;
                    _selectedChoiceIdx = -1;
                    _draggingNode      = node;
                    _draggingNodeOffset = nodeScreenPos - mousePos;
                    GUI.FocusControl(null);
                    Repaint();
                    e.Use();
                    return;
                }
            }

            // 3. Click on empty space — deselect
            _selectedNode      = null;
            _selectedChoiceIdx = -1;
            Repaint();
        }

        private void HandleContextMenu(Vector2 mousePos)
        {
            GenericMenu menu = new();

            EventNodeSO hitNode = GetNodeAtPos(mousePos);
            if (hitNode != null)
            {
                menu.AddItem(new GUIContent("Add Choice"), false, () => { AddChoiceToNode(hitNode); _selectedNode = hitNode; });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Set as Entry Node"), false, () => SetAsEntryNode(hitNode));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(hitNode));
            }
            else
            {
                Vector2 graphPos = ScreenToGraph(mousePos);
                menu.AddItem(new GUIContent("Create Node here"), false, () => CreateNodeAt(graphPos));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Auto Layout"), false, AutoLayoutNodes);
                menu.AddItem(new GUIContent("Frame All"),   false, FrameAllNodes);
            }

            menu.ShowAsContext();
        }

        private void FinishConnection(EventNodeSO target)
        {
            Undo.RecordObject(_connFromNode, "Connect Nodes");

            if (_connFromChoiceIdx >= 0 && _connFromChoiceIdx < _connFromNode.choices.Length)
            {
                var choice = _connFromNode.choices[_connFromChoiceIdx];
                if (choice != null)
                {
                    Undo.RecordObject(choice, "Connect Choice");
                    choice.nextNode = target;
                    EditorUtility.SetDirty(choice);
                }
            }
            else
            {
                _connFromNode.continueNode = target;
                EditorUtility.SetDirty(_connFromNode);
            }

            AssetDatabase.SaveAssets();
            Repaint();
        }

        // ════════════════════════════════════════════════════════════════════
        //  NODE / ASSET CREATION
        // ════════════════════════════════════════════════════════════════════
        private void CreateNode()
        {
            _nextNodePos += new Vector2(20, 20);
            if (_nextNodePos.x > 600) _nextNodePos = new Vector2(40, 40);
            CreateNodeAt(_nextNodePos);
        }

        private void CreateNodeAt(Vector2 graphPos)
        {
            if (_activeChain == null) return;

            string chainFolder = GetOrCreateChainFolder();
            string path        = AssetDatabase.GenerateUniqueAssetPath($"{chainFolder}/Node.asset");

            var node = CreateInstance<EventNodeSO>();
            node.name = "Node";

            AssetDatabase.CreateAsset(node, path);
            AssetDatabase.SaveAssets();

            _nodePositions[node] = graphPos;
            _allNodes.Add(node);
            SaveNodePositions();

            // If this is the first node, make it the entry node
            if (_activeChain.entryNode == null)
            {
                Undo.RecordObject(_activeChain, "Set Entry Node");
                _activeChain.entryNode = node;
                EditorUtility.SetDirty(_activeChain);
                AssetDatabase.SaveAssets();
            }

            _selectedNode = node;
            Repaint();
        }

        private void AddChoiceToNode(EventNodeSO node)
        {
            string chainFolder = GetOrCreateChainFolder();
            int idx            = node.choices?.Length ?? 0;
            string path        = AssetDatabase.GenerateUniqueAssetPath($"{chainFolder}/Choice_{node.name}_{idx}.asset");

            var choice = CreateInstance<ChoiceSO>();
            choice.name  = $"Choice_{node.name}_{idx}";
            choice.label = $"Option {idx + 1}";

            AssetDatabase.CreateAsset(choice, path);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(node, "Add Choice");

            var list = node.choices?.ToList() ?? new List<ChoiceSO>();
            list.Add(choice);
            node.choices = list.ToArray();

            // Clear continueNode when first choice is added
            if (node.choices.Length == 1)
                node.continueNode = null;

            EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void DeleteChoice(EventNodeSO node, int idx)
        {
            if (node.choices == null || idx >= node.choices.Length) return;

            var choice = node.choices[idx];
            Undo.RecordObject(node, "Delete Choice");

            var list = node.choices.ToList();
            list.RemoveAt(idx);
            node.choices = list.ToArray();
            EditorUtility.SetDirty(node);

            if (choice != null)
            {
                string choicePath = AssetDatabase.GetAssetPath(choice);
                if (!string.IsNullOrEmpty(choicePath))
                    AssetDatabase.DeleteAsset(choicePath);
            }

            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void DeleteNode(EventNodeSO node)
        {
            // Remove all references from other nodes
            foreach (var n in _allNodes)
            {
                if (n == null || n == node) continue;
                Undo.RecordObject(n, "Delete Node Reference");
                if (n.continueNode == node) n.continueNode = null;
                if (n.choices != null)
                    foreach (var c in n.choices)
                        if (c != null && c.nextNode == node) c.nextNode = null;
                EditorUtility.SetDirty(n);
            }

            // Remove from chain entry node
            if (_activeChain.entryNode == node)
            {
                Undo.RecordObject(_activeChain, "Remove Entry Node");
                _activeChain.entryNode = null;
                EditorUtility.SetDirty(_activeChain);
            }

            _allNodes.Remove(node);
            _nodePositions.Remove(node);

            if (_selectedNode == node) _selectedNode = null;

            // Delete the asset
            string path = AssetDatabase.GetAssetPath(node);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.SaveAssets();
            SaveNodePositions();
            Repaint();
        }

        private void SetAsEntryNode(EventNodeSO node)
        {
            Undo.RecordObject(_activeChain, "Set Entry Node");
            _activeChain.entryNode = node;
            EditorUtility.SetDirty(_activeChain);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void CreateAndAssignOutcome<T>(EventNodeSO node, string label) where T : EventOutcomeSO
        {
            string chainFolder = GetOrCreateChainFolder();
            string path        = AssetDatabase.GenerateUniqueAssetPath($"{chainFolder}/Outcome_{label}_{node.name}.asset");

            var outcome = CreateInstance<T>();
            outcome.name = $"Outcome_{label}_{node.name}";

            AssetDatabase.CreateAsset(outcome, path);

            Undo.RecordObject(node, "Assign Outcome");
            node.outcome = outcome;
            EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        // ════════════════════════════════════════════════════════════════════
        //  CHAIN MANAGEMENT
        // ════════════════════════════════════════════════════════════════════
        private void DrawNewChainModal()
        {
            Rect modalRect = new(position.width / 2 - 160, position.height / 2 - 60, 320, 120);
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0, 0, 0, 0.4f));
            EditorGUI.DrawRect(modalRect, new Color(0.22f, 0.22f, 0.22f));
            EditorGUI.DrawRect(new Rect(modalRect.x, modalRect.y, modalRect.width, 28), ColHeader);

            GUI.Label(new Rect(modalRect.x + 8, modalRect.y + 6, modalRect.width - 16, 18), "New Event Chain", EditorStyles.boldLabel);

            GUI.Label(new Rect(modalRect.x + 8, modalRect.y + 36, 80, 20), "Chain Name");
            _newChainName = GUI.TextField(new Rect(modalRect.x + 90, modalRect.y + 36, modalRect.width - 98, 20), _newChainName);

            if (GUI.Button(new Rect(modalRect.x + 8, modalRect.y + 80, 140, 26), "Create"))
            {
                CreateNewChain(_newChainName);
                _showNewChainPopup = false;
            }

            if (GUI.Button(new Rect(modalRect.x + 160, modalRect.y + 80, 140, 26), "Cancel"))
                _showNewChainPopup = false;

            // Close on Escape
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _showNewChainPopup = false;
                Event.current.Use();
            }
        }

        private void CreateNewChain(string chainName)
        {
            string basePath = "Assets/CK2Events/Chains";
            if (!AssetDatabase.IsValidFolder("Assets/CK2Events"))
                AssetDatabase.CreateFolder("Assets", "CK2Events");
            if (!AssetDatabase.IsValidFolder(basePath))
                AssetDatabase.CreateFolder("Assets/CK2Events", "Chains");

            string folderPath = AssetDatabase.GenerateUniqueAssetPath($"{basePath}/{chainName}");
            AssetDatabase.CreateFolder(basePath, Path.GetFileName(folderPath));

            string chainPath = $"{folderPath}/{chainName}.asset";
            var chain        = CreateInstance<EventChainSO>();
            chain.name       = chainName;
            chain.chainId    = chainName.ToLower().Replace(" ", "_");
            chain.defaultTitle = chainName;

            AssetDatabase.CreateAsset(chain, chainPath);
            AssetDatabase.SaveAssets();

            RefreshChainList();
            LoadChain(chain);
        }

        private void LoadChain(EventChainSO chain)
        {
            _activeChain       = chain;
            _selectedNode      = null;
            _selectedChoiceIdx = -1;
            _allNodes.Clear();
            _nodePositions.Clear();

            // Discover all nodes in the chain folder
            string chainPath = AssetDatabase.GetAssetPath(chain);
            string folder    = Path.GetDirectoryName(chainPath);

            LoadNodePositions(chain);

            string[] guids = AssetDatabase.FindAssets("t:EventNodeSO", new[] { folder });
            foreach (var guid in guids)
            {
                var node = AssetDatabase.LoadAssetAtPath<EventNodeSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (node != null)
                {
                    _allNodes.Add(node);
                    if (!_nodePositions.ContainsKey(node))
                        _nodePositions[node] = new Vector2(40 + _allNodes.Count * 220, 40);
                }
            }

            FrameAllNodes();
            Repaint();
        }

        private void RefreshChainList()
        {
            _lastChainRefresh = EditorApplication.timeSinceStartup;
            string[] guids    = AssetDatabase.FindAssets("t:EventChainSO");
            _allChains = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<EventChainSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null)
                .OrderBy(c => c.name)
                .ToList();
        }

        // ════════════════════════════════════════════════════════════════════
        //  LAYOUT HELPERS
        // ════════════════════════════════════════════════════════════════════
        private void AutoLayoutNodes()
        {
            if (_activeChain?.entryNode == null || _allNodes.Count == 0) return;

            // BFS layout
            var visited  = new HashSet<EventNodeSO>();
            var queue    = new Queue<(EventNodeSO node, int col, int row)>();
            var colCount = new Dictionary<int, int>();

            queue.Enqueue((_activeChain.entryNode, 0, 0));

            while (queue.Count > 0)
            {
                var (node, col, row) = queue.Dequeue();
                if (node == null || visited.Contains(node)) continue;
                visited.Add(node);

                if (!colCount.ContainsKey(col)) colCount[col] = 0;
                int r = colCount[col]++;

                _nodePositions[node] = new Vector2(col * (NodeW + 60), r * (NodeH + 80 + node.choices?.Length * ChoicePortH ?? 0));

                // Enqueue children
                if (node.HasChoices)
                    foreach (var ch in node.choices)
                        if (ch?.nextNode != null && !visited.Contains(ch.nextNode))
                            queue.Enqueue((ch.nextNode, col + 1, r));

                if (node.continueNode != null && !visited.Contains(node.continueNode))
                    queue.Enqueue((node.continueNode, col + 1, r));
            }

            SaveNodePositions();
            FrameAllNodes();
            Repaint();
        }

        private void FrameAllNodes()
        {
            if (_nodePositions.Count == 0)
            {
                _graphOffset = Vector2.zero;
                _zoom = 1f;
                return;
            }

            float graphW = position.width - SidebarW - InspectorW;
            float graphH = position.height - TopbarH;

            float minX = _nodePositions.Values.Min(v => v.x);
            float minY = _nodePositions.Values.Min(v => v.y);
            float maxX = _nodePositions.Values.Max(v => v.x) + NodeW;
            float maxY = _nodePositions.Values.Max(v => v.y) + NodeH + 40;

            float contentW = maxX - minX;
            float contentH = maxY - minY;

            // Fit zoom so content fills ~80% of the canvas
            float zoomX = graphW * 0.85f / Mathf.Max(contentW, 1f);
            float zoomY = graphH * 0.85f / Mathf.Max(contentH, 1f);
            _zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), ZoomMin, ZoomMax);

            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;
            _graphOffset = new Vector2(graphW / 2f - centerX * _zoom, graphH / 2f - centerY * _zoom);

            Repaint();
        }

        // ════════════════════════════════════════════════════════════════════
        //  PORT & POSITION UTILITIES
        // ════════════════════════════════════════════════════════════════════
        private Vector2 GetNodePos(EventNodeSO node)
        {
            if (!_nodePositions.TryGetValue(node, out Vector2 pos))
                pos = Vector2.zero;
            return pos;
        }

        private float GetNodeHeight(EventNodeSO node)
        {
            float h = NodeH;
            if (node.HasChoices) h += node.choices.Length * (ChoicePortH + 4);
            if (node.isEndNode && node.outcome != null) h += 20;
            return h;
        }

        private Vector2 GetInputPortPos(EventNodeSO node)
        {
            Vector2 screenOrigin = GraphToScreen(GetNodePos(node));
            float   scaledH      = GetNodeHeight(node) * _zoom;
            return new Vector2(screenOrigin.x, screenOrigin.y + scaledH / 2f);
        }

        private Vector2 GetOutputPortPos(EventNodeSO node, int choiceIdx)
        {
            Vector2 screenOrigin = GraphToScreen(GetNodePos(node));
            float   scaledW      = NodeW * _zoom;

            if (choiceIdx < 0)
            {
                float scaledH = GetNodeHeight(node) * _zoom;
                return new Vector2(screenOrigin.x + scaledW, screenOrigin.y + scaledH / 2f);
            }

            // Choice ports — scale the offsets
            float choiceStartY = screenOrigin.y + NodeH * _zoom + 4f * _zoom;
            float portY        = choiceStartY + choiceIdx * (ChoicePortH + 4f) * _zoom + ChoicePortH * _zoom / 2f;
            return new Vector2(screenOrigin.x + scaledW, portY);
        }

        private Vector2 GraphToScreen(Vector2 graphPos) => graphPos * _zoom + _graphOffset;
        private Vector2 ScreenToGraph(Vector2 screenPos) => (screenPos - _graphOffset) / _zoom;

        private bool HitPort(Vector2 mouse, Vector2 portPos) =>
            Vector2.Distance(mouse, portPos) < PortRadius + 4f;

        private bool HitAnyNode(Vector2 mousePos)
        {
            foreach (var node in _allNodes)
            {
                if (node == null) continue;
                Vector2 p = GraphToScreen(GetNodePos(node));
                if (new Rect(p.x, p.y, NodeW * _zoom, GetNodeHeight(node) * _zoom).Contains(mousePos))
                    return true;
            }
            return false;
        }

        private EventNodeSO GetNodeAtPos(Vector2 mousePos)
        {
            foreach (var node in _allNodes.ToList().AsEnumerable().Reverse())
            {
                if (node == null) continue;
                Vector2 p = GraphToScreen(GetNodePos(node));
                if (new Rect(p.x, p.y, NodeW * _zoom, GetNodeHeight(node) * _zoom).Contains(mousePos))
                    return node;
            }
            return null;
        }

        private EventNodeSO GetNodeAtInputPort(Vector2 mousePos)
        {
            foreach (var node in _allNodes)
            {
                if (node == null) continue;
                if (HitPort(mousePos, GetInputPortPos(node)))
                    return node;
            }
            return null;
        }

        // ════════════════════════════════════════════════════════════════════
        //  NODE POSITION PERSISTENCE (EditorPrefs)
        // ════════════════════════════════════════════════════════════════════
        private string PositionPrefKey(EventChainSO chain) =>
            $"CK2Events_NodePos_{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(chain))}";

        [Serializable]
        private class NodePosEntry { public string guid; public float x; public float y; }
        [Serializable]
        private class NodePosList  { public List<NodePosEntry> entries = new(); }

        private void SaveNodePositions()
        {
            if (_activeChain == null) return;

            var list = new NodePosList();
            foreach (var kv in _nodePositions)
            {
                if (kv.Key == null) continue;
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(kv.Key));
                if (!string.IsNullOrEmpty(guid))
                    list.entries.Add(new NodePosEntry { guid = guid, x = kv.Value.x, y = kv.Value.y });
            }

            EditorPrefs.SetString(PositionPrefKey(_activeChain), JsonUtility.ToJson(list));
        }

        private void LoadNodePositions(EventChainSO chain)
        {
            string json = EditorPrefs.GetString(PositionPrefKey(chain), "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var list = JsonUtility.FromJson<NodePosList>(json);
                foreach (var entry in list.entries)
                {
                    string path = AssetDatabase.GUIDToAssetPath(entry.guid);
                    var    node = AssetDatabase.LoadAssetAtPath<EventNodeSO>(path);
                    if (node != null)
                        _nodePositions[node] = new Vector2(entry.x, entry.y);
                }
            }
            catch { /* stale data, ignore */ }
        }

        // ════════════════════════════════════════════════════════════════════
        //  FOLDER UTILITY
        // ════════════════════════════════════════════════════════════════════
        private string GetOrCreateChainFolder()
        {
            string chainAssetPath = AssetDatabase.GetAssetPath(_activeChain);
            return Path.GetDirectoryName(chainAssetPath).Replace('\\', '/');
        }

        // ════════════════════════════════════════════════════════════════════
        //  INSPECTOR HELPERS
        // ════════════════════════════════════════════════════════════════════
        private static void InspSection(string label)
        {
            GUILayout.Space(4);
            Rect r = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
            GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 16, 18), label,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            GUILayout.Space(2);
        }

        private static void InspField(SerializedObject so, string propName, string label)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
        }
    }
}
#endif
