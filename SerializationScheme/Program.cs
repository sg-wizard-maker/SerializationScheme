using Newtonsoft.Json;

namespace SerializationScheme
{
    public class Program
    {

        static void Main(string[] args)
        {
            //Console.WriteLine("Hello, World!");

            var RegistrarSimpleArchetypalDataEntity = new ObjRegistrar<SimpleArchetypalDataEntity>();
            var RegistrarSimpleInstanceDataEntity = new ObjRegistrar<SimpleInstanceDataEntity>();
            var RegistrarComplexInstanceDataEntities = new ObjRegistrar<ComplexInstanceDataEntity>();

            #region Simple Archetypeal/Instance entities
            var arch1 = new SimpleArchetypalDataEntity(1, "sval_A", "arch_arch1", null);
            var arch2 = new SimpleArchetypalDataEntity(2, "sval_B", "arch_arch2", null);

            RegistrarSimpleArchetypalDataEntity.RegisterObj(arch1);  // Belongs in ctor
            RegistrarSimpleArchetypalDataEntity.RegisterObj(arch2);  // Belongs in ctor

            var instance1 = new SimpleInstanceDataEntity(arch1, 11, "abc", "i_First",  null);
            var instance2 = new SimpleInstanceDataEntity(arch1, 22, "def", "i_Second", null);
            var instance3 = new SimpleInstanceDataEntity(arch2, 33, "ghi", "i_Third",  null);

            RegistrarSimpleInstanceDataEntity.RegisterObj(instance1);  // Belongs in ctor
            RegistrarSimpleInstanceDataEntity.RegisterObj(instance2);  // Belongs in ctor
            RegistrarSimpleInstanceDataEntity.RegisterObj(instance3);  // Belongs in ctor

            // We now have some simple Archetypal/Instance data set up, so let's serialize:
            string arch1JSON     = CustomSerializer.SerializeJustThisObject(arch1);
            string instance1JSON = CustomSerializer.SerializeJustThisObject(instance1);

            // These 3: results in anonymous obj, all same results
            // Result maybe useful as a POCO, but in the end we want an actual SimpleArchetypalDataEntity
            var o1 = arch1.BasicDeserializationToPOCO(SimpleArchetypalDataEntity.ExampleJsonCompact);
            var o2 = arch1.BasicDeserializationToPOCO(SimpleArchetypalDataEntity.ExampleJsonMultiLineIndented);
            var o3 = arch1.BasicDeserializationToPOCO(arch1JSON);

            // These 3: results in SimpleArchetypalDataEntity, all same results
            // Result OK for this instance, but will not suffice for List<>, Dictionary<>, refs-as-tag purposes
            var a1 = arch1.BasicDeserializationVisDeserializeObject(SimpleArchetypalDataEntity.ExampleJsonCompact);
            var a2 = arch1.BasicDeserializationVisDeserializeObject(SimpleArchetypalDataEntity.ExampleJsonMultiLineIndented);
            var a3 = arch1.BasicDeserializationVisDeserializeObject(arch1JSON);

            // Test deserialization (and lookup reference from Tag) for SimpleInstanceDataEntity:
            // TODO: Depends on implementing that method for SimpleInstanceDataEntity, so focusing on the ComplexInstanceDataEntity for now...
            // 
            //RegistrarSimpleInstanceDataEntity.ClearRegistrar();
            //var foo = SimpleInstanceDataEntity.DeserializeFromJson(instance1JSON);  // Should lookup reference to arch1 from the ObjRegistrar...

            #endregion


            #region Complex (composite) entities
            var firstComplex = new ComplexInstanceDataEntity(arch1, instance1, "ci_First", null);

            RegistrarComplexInstanceDataEntities.RegisterObj(firstComplex);

            // List<T> of primitive types
            firstComplex.ListOfInts.AddRange(new int[] { 2, 4, 6, 8, 10 });
            firstComplex.ListOfStrings.AddRange(new string[] { "aa", "bb", "cc", "dd" });

            // List<T> of reference types
            firstComplex.AddRangeArchetypeToList(arch1, arch2);
            firstComplex.AddRangeInstanceToList(instance1, instance2, instance3);

            // Dictionary<string,T> of primitive types
            firstComplex.DictionaryOfInts.Add("Ones", 1111);
            firstComplex.DictionaryOfInts.Add("Twos", 2222);

            firstComplex.DictionaryOfStrings.Add("a", "apple");
            firstComplex.DictionaryOfStrings.Add("b", "banana");
            firstComplex.DictionaryOfStrings.Add("c", "carrot");

            var dictionaryOfArchetypes = new Dictionary<string, SimpleArchetypalDataEntity>()
            {
                { "AlphaA", new SimpleArchetypalDataEntity(1, "aaa", "arch_AlphaA") },
                { "BetaA",  new SimpleArchetypalDataEntity(2, "bbb", "arch_BetaA") },
            };

            var dictionaryOfInstances = new Dictionary<string, SimpleInstanceDataEntity>()
            {
                { "AlphaI", new SimpleInstanceDataEntity(arch1, 4, "ddd", "i_AlphaI") },
                { "BetaI",  new SimpleInstanceDataEntity(arch2, 5, "eee", "i_BetaI") },
                { "CappaI", new SimpleInstanceDataEntity(arch1, 6, "fff", "i_CappaI") },
            };

            // Dictionary<string,T> of reference types
            firstComplex.AddArchetypesToDictionary(dictionaryOfArchetypes);
            firstComplex.AddInstancesToDictionary(dictionaryOfInstances);

            // We now have a complex/composite structure set up, so let's serialize:
            string complex1Json = CustomSerializer.SerializeJustThisObject(firstComplex);

            // Let's pretend this is a new session, and we just loaded archetype and instance data,
            // and are ready to deserialize a complex entity...
            //
            //RegistrarSimpleArchetypalDataEntity.ClearRegistrar();  // archetypes already in place (static data or deserialized, then registered)
            //RegistrarSimpleInstanceDataEntity.ClearRegistrar();    // same for simple instance data
            RegistrarComplexInstanceDataEntities.ClearRegistrar();   // Start with a clear slate for this type

            var complex1_deserialized = firstComplex.DeserializeFromJson(complex1Json);
            #endregion
        }
    }
}
