using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EdgeView = UnityEditor.Experimental.GraphView.Edge;

namespace Physalia.AbilitySystem.GraphViewEditor
{
    public class AbilityGraphEditorWindow : EditorWindow
    {
        private static readonly string DEFAULT_FOLDER_PATH = "Assets/";

        // Since we need to use names to find the correct VisualTreeAsset to replace,
        // these names should be corresponding to the contents of the UXML file.
        private static readonly string FILE_FIELD_NAME = "file-field";
        private static readonly string NEW_BUTTON_NAME = "new-button";
        private static readonly string SAVE_BUTTON_NAME = "save-button";
        private static readonly string RELOAD_BUTTON_NAME = "reload-button";
        private static readonly string GRAPH_VIEW_PARENT_NAME = "graph-view-parent";
        private static readonly string GRAPH_VIEW_NAME = "graph-view";

        [SerializeField]
        private VisualTreeAsset uiAsset = null;
        [HideInInspector]
        [SerializeField]
        private TextAsset currentFile = null;

        private ObjectField objectField;
        private AbilityGraphView graphView;

        [MenuItem("Tools/Physalia/Ability Graph Editor (GraphView) &1")]
        private static void Open()
        {
            AbilityGraphEditorWindow window = GetWindow<AbilityGraphEditorWindow>("Ability Graph Editor");
            window.Show();
        }

        private void OnEnable()
        {
            if (uiAsset == null)
            {
                Debug.LogError($"[{nameof(AbilityGraphEditorWindow)}] Missing UIAsset, set the corrent reference in {nameof(AbilityGraphEditorWindow)} ScriptAsset might fix this");
                return;
            }

            uiAsset.CloneTree(rootVisualElement);

            objectField = rootVisualElement.Query<ObjectField>(FILE_FIELD_NAME).First();
            objectField.objectType = typeof(TextAsset);
            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.previousValue == evt.newValue)
                {
                    return;
                }

                if (evt.newValue == null)
                {
                    objectField.SetValueWithoutNotify(evt.previousValue);
                    return;
                }

                bool ok = AskForSave();
                if (ok)
                {
                    var textAsset = evt.newValue as TextAsset;
                    bool success = LoadFile(textAsset);
                    if (!success)
                    {
                        objectField.SetValueWithoutNotify(evt.previousValue);
                    }
                    else
                    {
                        currentFile = textAsset;
                    }
                }
                else
                {
                    objectField.SetValueWithoutNotify(evt.previousValue);
                }
            });

            Button newButton = rootVisualElement.Query<Button>(NEW_BUTTON_NAME).First();
            newButton.clicked += OnNewButtonClicked;

            Button saveButton = rootVisualElement.Query<Button>(SAVE_BUTTON_NAME).First();
            saveButton.clicked += SaveFile;

            Button reloadButton = rootVisualElement.Query<Button>(RELOAD_BUTTON_NAME).First();
            reloadButton.clicked += ReloadFile;

            if (currentFile == null)
            {
                NewGraphView();
            }
            else
            {
                objectField.SetValueWithoutNotify(currentFile);
                LoadFile(currentFile);
            }
        }

        private bool LoadFile(TextAsset textAsset)
        {
            string filePath = AssetDatabase.GetAssetPath(textAsset);
            AbilityGraph abilityGraph = AbilityGraphEditorIO.Read(filePath);
            if (abilityGraph == null)
            {
                return false;
            }

            AbilityGraphView graphView = AbilityGraphView.Create(abilityGraph);
            SetUpGraphView(graphView);
            return true;
        }

        private void ReloadFile()
        {
            var textAsset = objectField.value as TextAsset;
            if (textAsset == null)
            {
                return;
            }

            bool ok = AskForSave();
            if (ok)
            {
                LoadFile(textAsset);
            }
        }

        private void SaveFile()
        {
            string filePath;
            var textAsset = objectField.value as TextAsset;
            if (textAsset == null)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save ability", "NewAbility", "json",
                    "Please enter a file name to save to", DEFAULT_FOLDER_PATH);
                if (path.Length == 0)
                {
                    return;
                }

                filePath = path;
            }
            else
            {
                filePath = AssetDatabase.GetAssetPath(textAsset);
            }

            AbilityGraph abilityGraph = graphView.GetAbilityGraph();
            AbilityGraphEditorIO.Write(filePath, abilityGraph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (textAsset == null)
            {
                textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                objectField.SetValueWithoutNotify(textAsset);
                currentFile = textAsset;
            }
        }

        private void OnNewButtonClicked()
        {
            bool ok = AskForSave();
            if (ok)
            {
                NewGraphView();
            }
        }

        private bool AskForSave()
        {
            int option = EditorUtility.DisplayDialogComplex("Unsaved Changes",
                "Do you want to save the changes you made before quitting?",
                "Save",
                "Cancel",
                "Don't Save");

            switch (option)
            {
                default:
                    Debug.LogError("Unrecognized option");
                    return false;
                // Save
                case 0:
                    SaveFile();
                    return true;
                // Cancel
                case 1:
                    return false;
                // Don't Save
                case 2:
                    return true;
            }
        }

        private void NewGraphView()
        {
            SetUpGraphView(new AbilityGraphView());
            objectField.SetValueWithoutNotify(null);
            currentFile = null;
        }

        private void SetUpGraphView(AbilityGraphView graphView)
        {
            if (this.graphView != null)
            {
                this.graphView.RemoveFromHierarchy();
            }

            this.graphView = graphView;
            graphView.name = GRAPH_VIEW_NAME;

            VisualElement graphViewParent = rootVisualElement.Query<VisualElement>(GRAPH_VIEW_PARENT_NAME).First();
            graphViewParent.Add(graphView);

            graphView.graphViewChanged += OnGraphViewChanged;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                foreach (GraphElement element in graphViewChange.elementsToRemove)
                {
                    if (element is NodeView node)
                    {
                        AbilityGraph abilityGraph = graphView.GetAbilityGraph();
                        abilityGraph.RemoveNode(node.Node);
                    }
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (EdgeView edgeView in graphViewChange.edgesToCreate)
                {
                    var outputNodeView = edgeView.output.node as NodeView;
                    var inputNodeView = edgeView.input.node as NodeView;
                    Port outport = outputNodeView.GetPort(edgeView.output);
                    Port inport = inputNodeView.GetPort(edgeView.input);
                    outport.Connect(inport);
                }
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (GraphElement element in graphViewChange.movedElements)
                {
                    if (element is NodeView nodeView)
                    {
                        nodeView.Node.position = element.GetPosition().position;
                    }
                }
            }

            return graphViewChange;
        }
    }
}