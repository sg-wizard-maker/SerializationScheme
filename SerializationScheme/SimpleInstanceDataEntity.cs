﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace SerializationScheme
{
    public class SimpleInstanceDataEntity : IObjForRegistrar
    {
        //static bool SerializeRefAsGuid = false;
        //static bool SerializeRefAsTag  = true;

        #region Members for IObjForRegistrar
        public Guid   Id  { get; set; }
        public string Tag { get; set; }
        #endregion

        [JsonProperty(ItemIsReference = true)]
        public SimpleArchetypalDataEntity Archetype { get; private set; }

        [JsonProperty(ItemIsReference = true)]
        public SimpleArchetypalDataEntity ArchetypeTwo { get; private set; }

        public int     InstanceIntValue     { get; set; }
        public string  InstanceStringValue  { get; set; } = "";

        public SimpleInstanceDataEntity(SimpleArchetypalDataEntity arch, int ival, string sval, string tag, Guid? existingGuid = null)
        {
            #region IObjForRegistrar
            this.Id = existingGuid ?? Guid.NewGuid();
            this.Tag = tag;
            #endregion

            this.Archetype           = arch;
            this.ArchetypeTwo        = arch;
            this.InstanceIntValue    = ival;
            this.InstanceStringValue = sval;
        }

        public void TestSerializationViaJsonSerializer()
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            serializer.Serialize(writer, this);
            // Observed behavior:
            // Members marked as [JsonProperty(ItemIsReference = true)]
            // did not get any special handling;
            // had expected different behavior for 2nd+ such member handled...
            // 
            // Looks like manually handling the serialization is the fastest way to get the desired behaviors.
            writer.Flush();

            string ss = sb.ToString();
        }
    }
}
