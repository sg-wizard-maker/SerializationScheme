using Newtonsoft.Json;

namespace SerializationScheme
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            #region Simple Archetypeal/Instance entities
            var arch1 = new SimpleArchetypalDataEntity(1, "sval_A", "a_First",  null);
            var arch2 = new SimpleArchetypalDataEntity(2, "sval_B", "a_Second", null);

            var instance1 = new SimpleInstanceDataEntity(arch1, 11, "abc", "i_First",  null);
            var instance2 = new SimpleInstanceDataEntity(arch1, 22, "def", "i_Second", null);
            var instance3 = new SimpleInstanceDataEntity(arch2, 33, "ghi", "i_Third",  null);

            // We now have some simple Archetypal/Instance data set up, so let's serialize:
            string arch1JSON     = CustomSerializer.SerializeJustThisObject(arch1);
            string instance1JSON = CustomSerializer.SerializeJustThisObject(instance1);

            arch1.BasicDeserializationToPOCO(SimpleArchetypalDataEntity.ExampleJsonCompact);
            arch1.BasicDeserializationToPOCO(arch1JSON);
            arch1.BasicDeserializationVisDeserializeObject(SimpleArchetypalDataEntity.ExampleJsonCompact);
            arch1.BasicDeserializationVisDeserializeObject(arch1JSON);
            #endregion


            #region Complex (composite) entities
            var firstComplex = new ComplexInstanceDataEntity(arch1, instance1, "ci_First", null);

            firstComplex.AddRangeArchetypeToList(arch1, arch2);
            firstComplex.AddRangeInstanceToList(instance1, instance2, instance3);

            var dictionaryOfArchetypes = new Dictionary<string, SimpleArchetypalDataEntity>()
            {
                { "AlphaA", new SimpleArchetypalDataEntity(1, "aaa", "a_AAA") },
                { "BetaA",  new SimpleArchetypalDataEntity(2, "bbb", "a_BBB") },
            };

            var dictionaryOfInstances = new Dictionary<string, SimpleInstanceDataEntity>()
            {
                { "AlphaI", new SimpleInstanceDataEntity(arch1, 4, "ddd", "i_DDD") },
                { "BetaI",  new SimpleInstanceDataEntity(arch2, 5, "eee", "i_EEE") },
                { "CappaI", new SimpleInstanceDataEntity(arch1, 6, "fff", "i_FFF") },
            };

            firstComplex.AddArchetypesToDictionary(dictionaryOfArchetypes);
            firstComplex.AddInstancesToDictionary(dictionaryOfInstances);

            firstComplex.ListOfInts.AddRange( new int[] { 2, 4, 6, 8, 10 } );
            firstComplex.ListOfStrings.AddRange(new string[] { "aa", "bb", "cc", "dd" } );

            firstComplex.DictionaryOfInts.Add("Ones", 1111);
            firstComplex.DictionaryOfInts.Add("Twos", 2222);

            firstComplex.DictionaryOfStrings.Add("a", "apple");
            firstComplex.DictionaryOfStrings.Add("b", "banana");
            firstComplex.DictionaryOfStrings.Add("c", "carrot");

            // We now have a complex/composite structure set up, so let's serialize:
            CustomSerializer.SerializeJustThisObject(firstComplex);
            #endregion
        }
    }
}
