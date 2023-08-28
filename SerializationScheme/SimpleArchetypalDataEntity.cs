using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace SerializationScheme
{
    public class SimpleArchetypalDataEntity : IObjForRegistrar
    {
        public const string ExampleJsonCompact = """
            {"Id":"c5d2f9bc-7822-4dc8-81ab-330b4b6c3584","Tag":"a_First","IntValue":1,"StringValue":"sval_A"}
            """;

        public const string ExampleJsonMultiLineIndented = """
            {
            	"Id":          "c5d2f9bc-7822-4dc8-81ab-330b4b6c3584",
            	"Tag":         "a_First",
            	"IntValue":    1,
            	"StringValue": "sval_A"
            }
            """;

        #region Members for IObjForRegistrar
        public Guid   Id  { get; set; }
        public string Tag { get; set; } = "";
        #endregion

        #region Other members
        public int    IntValue    { get; set; }
        public string StringValue { get; set; } = "";

        // TODO: Possibly support the [JsonIgnore] attribute in our custom serialization scheme...
        //[JsonIgnore]  // This attribute indicates to skip a property or field when serializing
        //public int NotAppearingInThisSerializedResult { get; set; } = 99;
        //
        //[JsonIgnore]  // (This attribute works on Fields as well as Properties)
        //public int IntFieldThatShouldBeSkipped = 99;
        #endregion

        #region Constructor
        public SimpleArchetypalDataEntity(int ival, string sval, string tag, Guid? existingGuid = null)
        {
            #region IObjForRegistrar
            this.Id  = existingGuid ?? Guid.NewGuid();
            this.Tag = tag;
            #endregion

            this.IntValue    = ival;
            this.StringValue = sval;
        }

        public SimpleArchetypalDataEntity()
        {
            // Empty ctor
        }
        #endregion

        public object? BasicDeserializationToPOCO(string json)
        {
            //var sr     = new StringReader(json);
            //var reader = new JsonTextReader(sr);

            // Deserialize direction to an anonymous sort of POCO object -- not quite what we need
            object? obj = JsonConvert.DeserializeObject(json);
            return obj;
        }

        public SimpleArchetypalDataEntity? BasicDeserializationVisDeserializeObject(string json)
        {
            // Directly to the desired class -- this won't handle our references-as-tags scheme, however
            // (SimpleInstanceDataEntity would fail to get useful values in reference-type members)
            SimpleArchetypalDataEntity? arch = JsonConvert.DeserializeObject<SimpleArchetypalDataEntity>(json);
            return arch;
        }

        #region Previous serialization experiments
        public string TestSerializationViaJsonSerializer()
        {
            var serializer = new JsonSerializer();
            var sb         = new StringBuilder();
            var writer     = new StringWriter(sb);

            serializer.Serialize(writer, this);
            writer.Flush();

            string str = sb.ToString();
            return str;
        }

        public string DoSerializationViaHandCoded()
        {
            var sw = new StringWriter();
            var writer = new JsonTextWriter(sw);
            //writer.AutoCompleteOnClose  = true;
            //writer.Formatting           = Formatting.Indented;
            //writer.Indentation          = 4;
            //writer.StringEscapeHandling = StringEscapeHandling.Default;

            //writer.WriteComment("This is a comment");
            writer.WriteStartObject();

            #region Members of this class to serialize:
            writer.WritePropertyName("Id");
            writer.WriteValue(this.Id);

            writer.WritePropertyName("Tag");
            writer.WriteValue(this.Tag);

            writer.WritePropertyName("IntValue");
            writer.WriteValue(this.IntValue);

            writer.WritePropertyName("StringValue");
            writer.WriteValue(this.StringValue);
            #endregion

            writer.WriteEndObject();
            writer.Flush();

            string str = sw.ToString();
            return str;
        }
        #endregion
    }
}
