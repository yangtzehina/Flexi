using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Physalia.AbilitySystem
{
    public class NodeConverter : JsonConverter<Node>
    {
        private const string ID_KEY = "_id";
        private const string POSITION_KEY = "_position";
        private const string TYPE_KEY = "_type";

        private static Assembly[] assembliesCache;

        public override Node ReadJson(JsonReader reader, Type objectType, Node existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            Node node = CreateNodeInstance(jsonObject);

            // ID
            JToken idToken = jsonObject[ID_KEY];
            if (idToken == null)
            {
                node.id = 0;
            }
            else
            {
                node.id = (int)idToken;
            }

            // Position
            JToken positionToken = jsonObject[POSITION_KEY];
            if (positionToken == null)
            {
                node.position = new Vector2(0f, 0f);
            }
            else
            {
                node.position = positionToken.ToObject<Vector2>();
            }

            // Custom Fields
            FieldInfo[] fields = node.GetType().GetFields();
            for (var i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (field.IsStatic)
                {
                    continue;
                }

                Type fieldType = field.FieldType;

                if (fieldType.IsAbstract)
                {
                    continue;
                }

                if (fieldType.IsDefined(typeof(NonSerializedAttribute), true))
                {
                    continue;
                }

                if (fieldType.IsSubclassOf(typeof(Inport)))
                {
                    // If the inport is not defined, create a new instance.
                    if (field.GetValue(node) == null)
                    {
                        var inport = Activator.CreateInstance(fieldType) as Inport;
                        field.SetValue(node, inport);
                    }
                }
                else if (fieldType.IsSubclassOf(typeof(Outport)))
                {
                    // If the outport is not defined, create a new instance.
                    if (field.GetValue(node) == null)
                    {
                        var outport = Activator.CreateInstance(fieldType) as Outport;
                        field.SetValue(node, outport);
                    }
                }
                else if (fieldType.IsSubclassOf(typeof(Variable)))
                {
                    // Get the variable. If the variable is not defined, create a new instance.
                    if (field.GetValue(node) is not Variable variable)
                    {
                        variable = Activator.CreateInstance(fieldType) as Variable;
                        field.SetValue(node, variable);
                    }

                    JToken jsonToken = jsonObject[field.Name];
                    if (jsonToken == null)
                    {
                        continue;
                    }

                    variable.Value = jsonToken.ToObject(variable.ValueType, serializer);
                }
            }

            return node;
        }

        private static Node CreateNodeInstance(JObject jsonObject)
        {
            JToken typeToken = jsonObject[TYPE_KEY];
            if (typeToken == null)
            {
                Debug.LogError($"[{nameof(NodeConverter)}] Deserialize failed: Missing the type field");
                return new UndefinedNode();
            }

            string typeName = typeToken.ToString();
            Type type = GetTypeByName(typeName);
            if (type == null)
            {
                Debug.LogError($"[{nameof(NodeConverter)}] Deserialize failed: Cannot find the type from all assemblies, typeName: {typeName}");
                return new UndefinedNode();
            }

            return Activator.CreateInstance(type) as Node;
        }

        private static Type GetTypeByName(string typeName)
        {
            if (assembliesCache == null)
            {
                assembliesCache = AppDomain.CurrentDomain.GetAssemblies();
            }

            foreach (Assembly assembly in assembliesCache)
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, Node value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            // ID
            writer.WritePropertyName(ID_KEY);
            writer.WriteValue(value.id);

            // Position
            writer.WritePropertyName(POSITION_KEY);
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(value.position.x));
            writer.WriteValue(value.position.x);
            writer.WritePropertyName(nameof(value.position.y));
            writer.WriteValue(value.position.y);
            writer.WriteEndObject();

            // Node Type
            writer.WritePropertyName(TYPE_KEY);
            Type nodeType = value.GetType();
            writer.WriteValue(nodeType.FullName);

            // Custom Fields
            FieldInfo[] fields = nodeType.GetFields();
            for (var i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (field.IsStatic)
                {
                    continue;
                }

                Type fieldType = field.FieldType;

                if (fieldType.IsAbstract)
                {
                    continue;
                }

                if (fieldType.IsDefined(typeof(NonSerializedAttribute), true))
                {
                    continue;
                }

                if (fieldType.IsSubclassOf(typeof(Variable)))
                {
                    writer.WritePropertyName(field.Name);

                    // Get the variable. If the variable is not defined, create a new instance.
                    if (field.GetValue(value) is not Variable variable)
                    {
                        variable = Activator.CreateInstance(fieldType) as Variable;
                    }

                    serializer.Serialize(writer, variable.Value);
                }
            }

            writer.WriteEndObject();
        }
    }
}
