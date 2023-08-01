using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace SerializationScheme
{
    public class ComplexInstanceDataEntity : IObjForRegistrar
    {
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

        // Members containing List<SomeObjectReference>
        public List<SimpleArchetypalDataEntity> ListArchetypesB { get; set; } = new List<SimpleArchetypalDataEntity>();
        public List<SimpleInstanceDataEntity>   ListInstancesB  { get; set; } = new List<SimpleInstanceDataEntity>();

        // Members containing Dictionary<string, SomeObjectReference>
        public Dictionary<string, SimpleArchetypalDataEntity> DictionaryArchetypesC { get; set; } = new Dictionary<string, SimpleArchetypalDataEntity>();
        public Dictionary<string, SimpleInstanceDataEntity>   DictionaryInstancesC  { get; set; } = new Dictionary<string, SimpleInstanceDataEntity>();

        public List<int>    ListOfInts    { get; set; } = new List<int>();
        public List<string> ListOfStrings { get; set; } = new List<string>();

        public Dictionary<string, int>    DictionaryOfInts    { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> DictionaryOfStrings { get; set; } = new Dictionary<string, string>();
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

        #region Methods to add comtent
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

        public void TestSerializationViaJsonSerializer()
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            serializer.Serialize(writer, this);
            writer.Flush();

            string ss = sb.ToString();
        }
    }
}
