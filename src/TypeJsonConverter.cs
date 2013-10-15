using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aq.Json
{
    public class TypeJsonConverter : JsonConverter
    {
        public IReadOnlyDictionary<string, Type> Cache { get { return this._cache; } }

        public TypeJsonConverter()
        {
            this._cache = new Dictionary<string, Type>(8);
        }

        public TypeJsonConverter(int initialCacheCapacity)
        {
            this._cache = new Dictionary<string, Type>(initialCacheCapacity);
        }

        public TypeJsonConverter(IEnumerable<KeyValuePair<string, Type>> cache)
        {
            this._cache = new Dictionary<string, Type>(32);
            foreach (var kv in cache) {
                this._cache[kv.Key] = kv.Value;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return TypeOfType.IsAssignableFrom(objectType);
        }

        public override void WriteJson(
            JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            this.SerializeType(writer, (Type) value);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jobject = JObject.Load(reader);
            return this.DeserializeType(jobject);
        }

        private const int TYPE_SIMPLE = 1;
        private const int TYPE_GENERIC = 2;
        private const int TYPE_GENERIC_DEFINITION = 3;
        
        private static readonly Type TypeOfType = typeof (Type);
        private static readonly Dictionary<string, Assembly>
            Assemblies = new Dictionary<string, Assembly>(8);

        private readonly Dictionary<string, Type> _cache;

        private void SerializeType(
            JsonWriter writer, Type type)
        {
            var typeName = type.FullName;
            var assemblyName = type.Assembly.FullName;
            
            if (type.IsGenericTypeDefinition) {
                var genericArity = type.GetGenericArguments().Length;

                var hash = string.Concat(
                    "3:", assemblyName, ":",
                    typeName, ":", genericArity
                );

                writer.WriteStartObject();
                
                writer.WritePropertyName("hash");
                writer.WriteValue(hash);

                writer.WritePropertyName("type");
                writer.WriteValue(TYPE_GENERIC_DEFINITION);

                writer.WritePropertyName("assemblyName");
                writer.WriteValue(assemblyName);

                writer.WritePropertyName("typeName");
                writer.WriteValue(typeName);

                writer.WritePropertyName("genericArity");
                writer.WriteValue(genericArity);

                writer.WriteEndObject();

                this._cache[hash] = type;
            }
            else if (type.IsGenericType) {
                writer.WriteStartObject();

                writer.WritePropertyName("hash");
                writer.WriteNull();

                writer.WritePropertyName("type");
                writer.WriteValue(TYPE_GENERIC);

                writer.WritePropertyName("assemblyName");
                writer.WriteValue(assemblyName);

                writer.WritePropertyName("typeName");
                writer.WriteValue(typeName);

                writer.WritePropertyName("genericDef");
                this.SerializeType(writer, type.GetGenericTypeDefinition());

                writer.WritePropertyName("genericArgs");
                writer.WriteStartArray();
                foreach (var arg in type.GetGenericArguments()) {
                    this.SerializeType(writer, arg);
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
            else {
                var hash = string.Concat(
                    "1:", assemblyName, ":", typeName
                );

                writer.WriteStartObject();

                writer.WritePropertyName("hash");
                writer.WriteValue(hash);

                writer.WritePropertyName("type");
                writer.WriteValue(TYPE_SIMPLE);

                writer.WritePropertyName("assemblyName");
                writer.WriteValue(assemblyName);

                writer.WritePropertyName("typeName");
                writer.WriteValue(typeName);

                writer.WriteEndObject();

                this._cache[hash] = type;
            }
        }
        
        private Type DeserializeType(JObject jo)
        {
            Type result;
            var hash = jo.Value<string>("hash");
            if (hash != null) {
                if (this._cache.TryGetValue(hash, out result)) {
                    return result;
                }
            }
            result = null;

            var type = jo.Value<int>("type");
            switch (type) {
                case TYPE_SIMPLE: {
                    var assemblyName = jo.Value<string>("assemblyName");
                    var assembly = LoadAssembly(assemblyName);
                    var typeName = jo.Value<string>("typeName");
                    
                    result = assembly.GetType(typeName);
                    break;
                }
                case TYPE_GENERIC: {
                    var joDef = (JObject) jo.GetValue("genericDef");
                    var genericDef = this.DeserializeType(joDef);

                    var jaArgs = (JArray) jo.GetValue("genericArgs");
                    var genericArity = jaArgs.Count;
                    var genericArgs = new Type[genericArity];
                    for (var i = 0; i < genericArity; ++i) {
                        var joArg = (JObject) jaArgs[i];
                        genericArgs[i] = this.DeserializeType(joArg);
                    }

                    result = genericDef.MakeGenericType(genericArgs);
                    break;
                }
                case TYPE_GENERIC_DEFINITION: {
                    var assemblyName = jo.Value<string>("assemblyName");
                    var assembly = LoadAssembly(assemblyName);
                    var types = assembly.GetTypes();
                    var typeName = jo.Value<string>("typeName");
                    var genericArity = jo.Value<int>("genericArity");
                    
                    foreach (var t in types) {
                        if (!t.IsGenericTypeDefinition) {
                            continue;
                        }
                        if (t.FullName != typeName) {
                            continue;
                        }
                        var arity = t.GetGenericArguments().Length;
                        if (arity != genericArity) {
                            continue;
                        }
                        
                        result = t;
                        break;
                    }
                    break;
                }
            }

            if (hash != null && result != null) {
                this._cache[hash] = result;
            }
            return result;
        }

        private static Assembly LoadAssembly(string assemblyName)
        {
            Assembly assembly;
            if (!Assemblies.TryGetValue(assemblyName, out assembly)) {
                assembly = Assembly.Load(new AssemblyName(assemblyName));
                Assemblies[assemblyName] = assembly;
            }
            return assembly;
        } 
    }
}
