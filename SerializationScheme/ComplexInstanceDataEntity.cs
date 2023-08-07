using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SerializationScheme
{
    public class ComplexInstanceDataEntity : IObjForRegistrar
    {
        public const string ExampleJsonCompact = """
            {"Id":"3e63a135-c398-4236-b857-b74cd53c3933","Tag":"ci_First","ArchetypeA":"a_First","InstanceA":"i_First","ArchetypeNullableA":null,"InstanceNullableA":null,"ListArchetypesB":["a_First","a_Second"],"ListInstancesB":["i_First","i_Second","i_Third"],"DictionaryArchetypesC":{"AlphaA":"a_AAA","BetaA":"a_BBB"},"DictionaryInstancesC":{"AlphaI":"i_DDD","BetaI":"i_EEE","CappaI":"i_FFF"},"ListOfInts":[2,4,6,8,10],"ListOfStrings":["aa","bb","cc","dd"],"DictionaryOfInts":{"Ones":1111,"Twos":2222},"DictionaryOfStrings":{"a":"apple","b":"banana","c":"carrot"}}
            """;

        public const string ExampleJsonMultiLineIndented = """
            {
            	"Id":                    "3e63a135-c398-4236-b857-b74cd53c3933",
            	"Tag":                   "ci_First",
            	"ArchetypeA":            "a_First",
            	"InstanceA":             "i_First",
            	"ArchetypeNullableA":    null,
            	"InstanceNullableA":     null,
            	"ListArchetypesB":       [ "a_First", "a_Second" ],
            	"ListInstancesB":        [ "i_First", "i_Second","i_Third" ],
            	"DictionaryArchetypesC": { "AlphaA":"a_AAA", "BetaA":"a_BBB" },
            	"DictionaryInstancesC":  { "AlphaI":"i_DDD", "BetaI":"i_EEE", "CappaI":"i_FFF" },
            	"ListOfInts":            [ 2, 4, 6, 8, 10 ],
            	"ListOfStrings":         [ "aa", "bb", "cc", "dd" ],
            	"DictionaryOfInts":      { "Ones":1111, "Twos":2222 },
            	"DictionaryOfStrings":   { "a":"apple", "b":"banana", "c":"carrot" }
            }
            """;

        #region Members for IObjForRegistrar
        public Guid   Id  { get; set; }
        public string Tag { get; set; }
        #endregion

        #region Complex - contains other objects (singly, in lists, in dictionaries)
        // Members containing single object reference (NOT nullable)
        public SimpleArchetypalDataEntity ArchetypeA { get; set; }
        public SimpleInstanceDataEntity   InstanceA  { get; set; }

        // Members containing single object reference (nullable) -- just in case this difference will require some difference
        public SimpleArchetypalDataEntity? ArchetypeNullableA { get; set; }
        public SimpleInstanceDataEntity?   InstanceNullableA  { get; set; }

        // Members containing List<T> of some "primitive" type
        public List<int> ListOfInts { get; set; } = new List<int>();
        public List<string> ListOfStrings { get; set; } = new List<string>();

        // Members containing List<SomeObjectReference>
        public List<SimpleArchetypalDataEntity> ListArchetypesB { get; set; } = new List<SimpleArchetypalDataEntity>();
        public List<SimpleInstanceDataEntity>   ListInstancesB  { get; set; } = new List<SimpleInstanceDataEntity>();

        // Members containing Dictionary<string,T> containing some "primitive" type
        public Dictionary<string, int> DictionaryOfInts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> DictionaryOfStrings { get; set; } = new Dictionary<string, string>();

        // Members containing Dictionary<string, SomeObjectReference>
        public Dictionary<string, SimpleArchetypalDataEntity> DictionaryArchetypesC { get; set; } = new Dictionary<string, SimpleArchetypalDataEntity>();
        public Dictionary<string, SimpleInstanceDataEntity>   DictionaryInstancesC  { get; set; } = new Dictionary<string, SimpleInstanceDataEntity>();
        #endregion

        public ComplexInstanceDataEntity(SimpleArchetypalDataEntity arch, SimpleInstanceDataEntity instance, string tag, Guid? existingGuid = null)
        {
            #region IObjForRegistrar
            this.Id  = existingGuid ?? Guid.NewGuid();
            this.Tag = tag;
            #endregion

            this.ArchetypeA = arch;
            this.InstanceA  = instance;
        }

        #region Methods to add content
        public void AddRangeArchetypeToList(params SimpleArchetypalDataEntity[] archetypes)
        {
            this.ListArchetypesB.AddRange(archetypes);
        }

        public void AddRangeInstanceToList(params SimpleInstanceDataEntity[] instances)
        {
            this.ListInstancesB.AddRange(instances);
        }

        public void AddArchetypesToDictionary(Dictionary<string, SimpleArchetypalDataEntity> archetypes)
        {
            foreach (var key in archetypes.Keys)
            {
                this.DictionaryArchetypesC.Add(key, archetypes[key]);
            }
        }

        public void AddInstancesToDictionary(Dictionary<string, SimpleInstanceDataEntity> instances)
        {
            foreach (var key in instances.Keys)
            {
                this.DictionaryInstancesC.Add(key, instances[key]);
            }
        }
        #endregion

        #region Previous serialization experiments
        public void TestSerializationViaJsonSerializer()
        {
            var serializer = new JsonSerializer();
            var sb         = new StringBuilder();
            var writer     = new StringWriter(sb);

            serializer.Serialize(writer, this);
            writer.Flush();

            string ss = sb.ToString();
        }
        #endregion

        #region Deserialization
        public ComplexInstanceDataEntity? DeserializeFromJson(string json)
        {
            // In CustomSerializer.SerializeJustThisObject(),
            // we have an object (whose Type is unavailable at compile time, but can be examined through Reflection),
            //     and iterate through its' properties,
            //         taking values for each and serializing them.
            // 
            // Here, the situation is reversed, as
            // we have a specific Type (ComplexInstanceDataEntity) but no constructed object instance yet,
            //     and can iterate through those properties,
            //        taking values from the JSON and gathering them so aso to make a constructor call
            // 

            // Notes:
            // May want to consider the performance of various JSON-wrangling options.
            // There is a summary table at the bottom of
            // https://code-maze.com/csharp-deserialize-json-into-dynamic-object/
            // which suggests that ExpandoObject is comparatively slow; 
            // this should be weight against the convenience in handling
            // (avoiding writing many serialization methods would be useful, ideally a single static method for any class)
            // 
            // TODO: Compare various different approaches...

            #region Reflection upon POCO from JSON
            Console.WriteLine("POCO contents from JSON:");
            var poco = CustomSerializer.BasicDeserializationToPOCO(json);
            var pocoAsExpandoObject = poco as ExpandoObject;
            foreach (var property in pocoAsExpandoObject )
            {
                if (property.Value == null)
                {
                    // Case 0: NULL value
                    Console.WriteLine("- " + property.Key + ":\t (SomeType) " + "NULL");
                    continue;
                }
                var valueType = property.Value.GetType();
                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // Case 1: Values in a List<T>
                    var elementType = valueType.GenericTypeArguments[0];
                    Console.Write("- " + property.Key + ":\t" + "List<" + elementType.Name + "> ");
                    Console.Write("[ ");
                    foreach (var element in property.Value as IList)
                    {
                        Console.Write(element + ", ");
                    }
                    Console.WriteLine("]");
                }
                else if (valueType == typeof(ExpandoObject))
                {
                    // Case 2:
                    // Values in a JSON object, which is parsed into an ExpandoObject
                    // by JsonConvert.DeserializeObject<ExpandoObject>()
                    var expando = property.Value as ExpandoObject;
                    Console.Write("- " + property.Key + ":\t" + "ExpandoObject ");
                    Console.Write("{ ");
                    foreach (var aaa in expando)
                    {
                        Console.Write("" + aaa.Key + ": " + aaa.Value + ", ");
                    }
                    Console.WriteLine("}");
                }
                else
                {
                    // Case 3:
                    // Simple value (could be primitive, or IObjForRegistrar)
                    // Note that we cannot distinguish IObjForRegistrar at this point, since we have only the raw data from the JSON
                    Console.WriteLine("- " + property.Key + ":\t" + "(" + valueType.Name + ") " + property.Value);
                }
            }
            Console.WriteLine();
            #endregion

            #region Reflection upon the Type of this class
            Type thisObjType = this.GetType();
            BindingFlags   flags       = (BindingFlags.Public | BindingFlags.Instance);
            //FieldInfo[]  fields      = thisObjType.GetFields(flags);  // Using only Properties, rather than a mix of Fields and Properties
            PropertyInfo[] properties  = thisObjType.GetProperties(flags);

            Console.WriteLine("DeserializeFromJson()");
            Console.WriteLine("Type=" + thisObjType.Name);

            for (int ii = 0; ii < properties.Length; ii++)
            {
                var property     = properties[ii];
                var propertyName = property.Name;
                var propertyType = property.PropertyType;

                Console.Write("- " + propertyName + ":\t");

                // Case 1: (Does a case analogous to "value is NULL" from CustomSerializer.SerializeJustThisObject exist here?)
                // Is special handling needed if we get a NULL value for some property in the JSON ?
                // What about mismatch between (type says not nullable, JSON contains NULL value) ?

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // Case 2: Some kind of List<T>
                    Console.Write("List<" + propertyType.GenericTypeArguments[0].Name + ">");
                    //foreach (var item in (IEnumerable)xxxxx)  // TODO: Iterate through values from JSON
                    {
                        Console.WriteLine("\t[ TODO list elements ]");
                        // TODO: Get element value from JSON, add to List in constructed obj...
                    }
                    // TODO: Or possibly use an AddRange() method on the aforementioned list here...
                    continue;
                }
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    // Case 3: Some kind of Dictionary<SomeKeyType,T>
                    Console.Write("Dictionary<" + propertyType.GenericTypeArguments[0].Name + "," + propertyType.GenericTypeArguments[1].Name + ">");
                    //var dictionary = (IDictionary)xxxxx;
                    //foreach (var key in dictionary.Keys)  // TODO: Iterate through keys/values from JSON
                    {
                        Console.WriteLine("\t{ TODO dictionary keys: values }");
                        // TODO: Get element value from JSON, add to Dictionary in constructed obj...
                    }
                    continue;
                }
                // Case 4: A primitive type such as (int, string)
                bool isObjForRegistrar = propertyType.GetInterfaces().Contains(typeof(IObjForRegistrar));
                if (!isObjForRegistrar)
                {
                    Console.WriteLine("    (Primitive) " + propertyType.Name);
                    // TODO: Get value from JSON, stash in constructed obj
                    continue;
                }
                // Case 5: Some type implementing IObjForRegistrar
                Console.WriteLine("    (IObjForRegistrar) " + propertyType.Name);
                // TODO: Get value from JSON, stash in constructed obj
                continue;
            }
            #endregion

            ComplexInstanceDataEntity? result = null;
            return result;

        }
        #endregion
    }
}
