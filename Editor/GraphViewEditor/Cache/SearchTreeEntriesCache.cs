using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Physalia.AbilityFramework.GraphViewEditor
{
    internal static class SearchTreeEntriesCache
    {
        internal class Postprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                Rebuild(didDomainReload);
            }
        }

#if !SHOW_TEST_NODES
        private static readonly string TEST_ASSEMBLY_NAME = "Physalia.AbilityFramework.Editor.Tests";
#endif

        private static readonly List<SearchTreeEntry> searchTreeEntries = new();
        private static readonly List<SearchTreeEntry> nodeTreeEntries = new();
        private static readonly List<SearchTreeEntry> macroTreeEntries = new();

        internal static List<SearchTreeEntry> Get()
        {
            return searchTreeEntries;
        }

        private static void Rebuild(bool didDomainReload)
        {
            RebuildSearchTreeEntries(didDomainReload);
        }

        private static void RebuildSearchTreeEntries(bool didDomainReload)
        {
            if (didDomainReload)
            {
                RebuildNodeTreeEntries();
            }
            RebuildMacroTreeEntries();

            searchTreeEntries.Clear();
            searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent("Node")) { level = 0 });
            searchTreeEntries.AddRange(nodeTreeEntries);
            searchTreeEntries.AddRange(macroTreeEntries);
        }

        private static void RebuildNodeTreeEntries()
        {
            nodeTreeEntries.Clear();

            IReadOnlyList<Type> nodeTypes = GetAllNodeTypes();
            NodeTypeSearchTree searchTree = CreateNodeTypeSearchTree(nodeTypes);

            IEnumerator<NodeTypeSearchTree.Node> enumerator = searchTree.GetEnumerator();
            while (enumerator.MoveNext())
            {
                NodeTypeSearchTree.Node node = enumerator.Current;
                if (node.IsLeaf)
                {
                    nodeTreeEntries.Add(new SearchTreeEntry(new GUIContent(node.Text)) { level = node.Level, userData = node.Type });
                }
                else
                {
                    nodeTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent(node.Text)) { level = node.Level });
                }
            }
        }

        private static IReadOnlyList<Type> GetAllNodeTypes()
        {
            var nodeTypes = new List<Type>();
            foreach (Assembly assembly in ReflectionUtilities.GetAssemblies())
            {
#if !SHOW_TEST_NODES
                // Hide the test node types
                if (assembly.GetName().Name == TEST_ASSEMBLY_NAME)
                {
                    continue;
                }
#endif

                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Node)))
                    {
                        if (type.GetCustomAttribute<HideFromSearchWindow>() != null)
                        {
                            continue;
                        }

                        nodeTypes.Add(type);
                    }
                }
            }

            return nodeTypes;
        }

        private static NodeTypeSearchTree CreateNodeTypeSearchTree(IReadOnlyList<Type> nodeTypes)
        {
            var searchTree = new NodeTypeSearchTree();
            for (var i = 0; i < nodeTypes.Count; i++)
            {
                Type type = nodeTypes[i];
                NodeCategory nodeCategory = type.GetCustomAttribute<NodeCategory>();
                if (nodeCategory != null)
                {
                    var path = $"{nodeCategory.Name}/{type.Name}";
                    searchTree.Insert(path, type);
                }
                else
                {
                    searchTree.Insert(type.Name, type);
                }
            }

            return searchTree;
        }


        private static void RebuildMacroTreeEntries()
        {
            macroTreeEntries.Clear();

            macroTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent("Macros")) { level = 1 });

            Dictionary<string, string> macroKeyToNameTable = GetMacroKeyToNameTable();
            foreach (KeyValuePair<string, string> pair in macroKeyToNameTable)
            {
                string key = pair.Key;
                string name = pair.Value;
                macroTreeEntries.Add(new SearchTreeEntry(new GUIContent(name)) { level = 2, userData = key });
            }
        }


        private static Dictionary<string, string> GetMacroKeyToNameTable()
        {
            var macroKeyToNameTable = new Dictionary<string, string>();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(MacroAsset)}");
            var assetPaths = new string[guids.Length];
            for (var i = 0; i < guids.Length; i++)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name = assetPaths[i].Split('/')[^1];
                if (name.EndsWith(".asset"))
                {
                    name = name.Substring(0, name.Length - ".asset".Length);
                }

                string key = name;  // TODO
                macroKeyToNameTable.Add(key, name);
            }

            return macroKeyToNameTable;
        }
    }
}