using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SerializationScheme
{
    public class CustomSerializer
    {
        #region Infrastructure defining which primitive types are supported
        private static Type[] TypesSupportedByNewtonsoftJsonSerialization = new Type[]
        {
            // These are a subset of the (fundamental or primitive) Types supported by Newtonsoft.Json serialization.
            // This list derives from internal mambers in Newtonsoft.Json.Utilities, specifically
            //     'internal enum PrimitiveTypeCode' and
            //     'internal static class ConvertUtils'
            // Inconveniently for our purpose here, those are not exposed as 'public',
            // so we are obliged to re-implement the bit we want.
            //
            // Currently, there seems to be no reason to support various
            // infreqently-used or special-purpose types such as:
            //     SByte, Int16, Byte, Int64, Single, Double, BigInteger, Uri, Bytes, DBNull
            // 
            // If some reason to support these should arise, it should be a simple matter to add them.
            // 
            typeof(bool),           typeof(bool?),
            typeof(char),           typeof(char?),
            typeof(int),            typeof(int?),
            typeof(decimal),        typeof(decimal?),

            typeof(DateTime),       typeof(DateTime?),
            typeof(DateTimeOffset), typeof(DateTimeOffset?),
            typeof(TimeSpan),       typeof(TimeSpan?),

            typeof(Guid),           typeof(Guid?),

            typeof(string),  // The only reference class among this set, thus no nullable version is listed
        };

        public static bool IsSupportedTypeForSerialization(Type type)
        {
            bool result = TypesSupportedByNewtonsoftJsonSerialization.Contains(type);
            return result;
        }
        #endregion

        #region Serialization and helper methods
        public static string SerializeJustThisObject(object obj)
        {
            // TODO:
            // This method should use the same queries/logic to distinguish categories/cases
            // as seen in DeserializeSingleObjectViaReflection(), the better to reduce bug habitat...

            var sw = new StringWriter();
            var writer = new JsonTextWriter(sw);

            writer.WriteStartObject();

            Type thisObjType = obj.GetType();
            BindingFlags flags = (BindingFlags.Public | BindingFlags.Instance);
            //FieldInfo[]  fields      = type.GetFields(flags);  // Using only Properties, rather than a mix of Fields and Properties
            PropertyInfo[] properties = thisObjType.GetProperties(flags);

            #region Handle Public Properties (only)
            // TODO: May want a mechanism (such as attribute [JsonIgnore]) to skip some members
            // TODO: May want ability to specify that (some field) or (some private property) should be emitted
            //       Then again, perhaps the simplest convention is "public properties only"...
            for (int ii = 0; ii < properties.Length; ii++)
            {
                var property = properties[ii];
                var propertyName = property.Name;
                var value = property.GetValue(obj);  // BUG: Need to ensure that property has public getter AND setter...set-only crashes here

                writer.WritePropertyName(propertyName);
                // Case 1: Value is NULL
                if (value == null)
                {
                    writer.WriteNull();
                    continue;
                }
                var typeOfPropertyValue = value.GetType();

                // Case 2: Value is some kind of List<T>
                if (typeOfPropertyValue.IsGenericType && typeOfPropertyValue.GetGenericTypeDefinition() == typeof(List<>))
                {
                    CustomSerializer.EmitJsonForList(writer, property, value);
                    continue;
                }
                // Case 3: Value is some kind of Dictionary<SomeKeyType,T>
                if (typeOfPropertyValue.IsGenericType && typeOfPropertyValue.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    CustomSerializer.EmitJsonForDictionary(writer, property, value);
                    continue;
                }

                // Case 4: Value is a primitive type such as (int, string)
                IObjForRegistrar? registeredObj = value as IObjForRegistrar;
                if (registeredObj == null)
                {
                    // Usually one of the fundamental types;
                    // encountering SomeRandomClass would cause a JsonWriterException here -- whoops, ConvertUtils is 'internal'
                    // TODO:
                    // Test with Newtonsoft.Json.Utilities.ConvertUtils.GetTypeCode() 
                    // to avoid such an Exception.
                    // TODO:
                    // There might be some kind of non-List, non-Dictionary, non-IObjForRegister
                    // which we would want to recurse into (and thus serialize "in-line")...
                    // Perhaps a protocol (ISerializeThisObjectInline or somesuch)
                    // which such objects could implement, indicating that we serialize them in some way?
                    writer.WriteValue(value); // possible JsonWriterException if SomeRandomClass...
                    continue;
                }
                // Case 5: Value is some type implementing IObjForRegistrar, serialize the reference as a Tag
                // Recursing is NOT done, as we want to serialize JUST this object.
                // 
                // If we wanted references for SOME classes to serialize as Guid,
                // some kind of class-based value (static member, constant, etc) would be wanted,
                // to indicate how each class should be handled.
                // As is, we will serialize ALL references to IObjForRegistrar as the Tag for that object.
                writer.WriteValue(registeredObj.Tag);
                continue;
            }
            #endregion

            writer.WriteEndObject();
            writer.Flush();

            string str = sw.ToString();
            return str;
        }

        private static void EmitJsonForList(JsonTextWriter writer, PropertyInfo property, object obj)
        {
            // We can definitely cast to IEnumerable, since our caller checked that this is some type of List<>
            // But just in case...
            if (!property.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
            {
                throw new Exception("Impossible? EmitJsonForList() somehow got obj that is not a List<>");
            }
            writer.WriteStartArray();
            foreach (var item in (IEnumerable)obj)
            {
                IObjForRegistrar? regObj = item as IObjForRegistrar;
                if (regObj == null)
                {
                    // Usually one of the fundamental types;
                    // encountering SomeRandomClass would cause a JsonWriterException here -- whoops, ConvertUtils is 'internal'
                    // TODO:
                    // Test with Newtonsoft.Json.Utilities.ConvertUtils.GetTypeCode() 
                    // to avoid such an Exception.
                    // TODO:
                    // There might be some kind of non-List, non-Dictionary, non-IObjForRegister
                    // which we would want to recurse into (and thus serialize "in-line")...
                    writer.WriteValue(item); // possible JsonWriterException if SomeRandomClass...
                }
                else
                {
                    writer.WriteValue(regObj.Tag);
                }
            }
            writer.WriteEndArray();
        }

        private static void EmitJsonForDictionary(JsonTextWriter writer, PropertyInfo property, object obj)
        {
            // We can definitely cast to IDictionary, since our caller checked that this is some type of Dictionary<>
            // But just in case...
            if (!property.PropertyType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                throw new Exception("Impossible? EmitJsonForList() somehow got obj that is not a Dictionary<>");
            }
            writer.WriteStartObject();
            var dictionary = (IDictionary)obj;
            foreach (var key in dictionary.Keys)
            {
                string? stringKey = key as string;
                if (stringKey == null) { throw new ArgumentException("EmitJsonForDictionary() got non-string key"); }

                var value = dictionary[key];
                IObjForRegistrar? regObj = value as IObjForRegistrar;
                if (regObj == null)
                {
                    // Usually one of the fundamental types;
                    // encountering SomeRandomClass would cause a JsonWriterException here
                    // TODO:
                    // Test with Newtonsoft.Json.Utilities.ConvertUtils.GetTypeCode() -- whoops, ConvertUtils is 'internal'
                    // to avoid such an Exception.
                    // TODO:
                    // There might be some kind of non-List, non-Dictionary, non-IObjForRegister
                    // which we would want to recurse into (and thus serialize "in-line")...
                    writer.WritePropertyName(stringKey);
                    writer.WriteValue(value); // possible JsonWriterException if SomeRandomClass...
                }
                else
                {
                    writer.WritePropertyName(stringKey);
                    writer.WriteValue(regObj.Tag);
                }
            }
            writer.WriteEndObject();
        }
        #endregion

        #region Deserialization and helper methods
        public static object? BasicDeserializationToPOCO(string json)
        {
            // TODO:
            // The use of ExpandoObject may be inefficient and slow.
            // Consider using some other means, if the useful things can be done otherwise...
            // 
            // Deserialize directly to an anonymous sort of POCO object; useful as a precursor to other deserialization activities
            object? obj = JsonConvert.DeserializeObject<ExpandoObject>(json);
            return obj;
        }

        public static object? DeserializeSingleObjectViaReflection(Type type, string json)
        {
            // From 'type', get the zero-args constructor, and call it so we have a blank object to fill in:
            Type[] constructorArgTypes = new Type[] { /*empty*/ };
            var ctor = type.GetConstructor(constructorArgTypes);
            object?[]? ctorParameters = new object?[] { /*empty*/ };
            if (ctor == null)
            {
                // TODO:
                // Are there any odd cases, where something fancier should be done,
                // or for which this case could be avoided or handled better?
                throw new ArgumentException("Got Type without zero-arg constructor");
                //return null;
            }
            object? obj = ctor.Invoke(ctorParameters);

            // ...do reflection upon 'type' and upon the POCO deserialized from 'json', to fill out 'obj':
            // Similar to the body of ComplexInstanceDataEntity.ExperimentDeserializeFromJsonViaReflection() ...

            // Scheme:
            // We act upon public properties only, with no provisions (currently) to exclude certain properties.
            // We assume that a valid object can be constructed via setting Properties only, ignoring Fields and other member types.
            // Each property is categorized into one of the cases below:
            // Category 1:
            //         A - "Primitive" types which are supported by JsonConvert.DeserializeObject()
            //         B - Reference types which implement IObjForRegistrar
            //     not C - (NOT supported at this time) Other reference types, which do NOT implement IObjForRegistrar
            //             also: other types such as arrays, structs, enums, and so forth
            //                 TODO: some tests for various additional types such as the aforementioned
            // Category 2:
            //         A - List<T> where T is one of (A, B) from category 1
            //         B - Dictionary<string,T> where T is one of (A,B) from category 1
            //     not C - (NOT supported at this time) Other collection types; if there proves to be a need, support might be added
            //     not D - (NOT supported at this time) List<T>              where T is not among (Category 1: A or B), or
            //                                          Dictionary<string,T> where T is not among (Category 1: A or B), or
            //                                          any Dictionary with a non-string key type
            // 
            // We iterate over the list of public properties, ...

            BindingFlags flags = (BindingFlags.Public | BindingFlags.Instance);
            //FieldInfo[]  fields     = type.GetFields(flags);  // Using only Properties, rather than a mix of Fields and Properties
            PropertyInfo[] properties = type.GetProperties(flags);

            // TODO:
            // How best to test for / filter out, properties that lack a public setter?
            // Found that a non-"auto" property, having only a setter (using a private backing field)
            // produced an exception in CustomSerializer.SerializeJustThisObject()

            Console.WriteLine("CustomSerializer.DeserializeSingleObjectViaReflection()");
            Console.WriteLine("Type=" + type.Name);

            #region Queries for distinct sets of same-case properties 
            // Category 1 vs 2: Collection vs non-collection types
            var propertiesOfCollectionTypes =
            (
                from p in properties
                where p.PropertyType.GetInterfaces().Contains(typeof(ICollection))
                select p
            );
            var propertiesOfNonCollectionTypes =
            (
                from p in properties
                where p.PropertyType.GetInterfaces().Contains(typeof(ICollection)) == false
                select p
            );

            #region Category 1 cases (non-collection types)
            var listCase1A_SupportedPrimitiveTypes =
            (
                from p in propertiesOfNonCollectionTypes
                where
                    CustomSerializer.IsSupportedTypeForSerialization(p.PropertyType)
                select p
            );

            var listCase1B_IObjForRegistrar =
            (
                from p in properties
                where
                    p.PropertyType.GetInterfaces().Contains(typeof(IObjForRegistrar))
                select p
            );

            var listCase1C_OtherNonCollectionTypes =
            (
                // Neither case 1A nor 1B
                from p in propertiesOfNonCollectionTypes
                where
                    CustomSerializer.IsSupportedTypeForSerialization(p.PropertyType) == false &&
                    p.PropertyType.GetInterfaces().Contains(typeof(IObjForRegistrar)) == false
                select p
            );
            #endregion

            #region Category 2 cases (collection types):
            // 2A: List of supported primitive, List of IObjForRegistrar
            // NOT supported include:
            //     List of (some collection type),
            //     List of (non-primitive not implementing IObjForRegistrar)
            var listCase2A_ListOfSupportedType =
            (
                from p in propertiesOfCollectionTypes
                where
                    p.PropertyType.IsGenericType &&
                    (
                        p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                        p.PropertyType.GetGenericTypeDefinition() != typeof(Dictionary<,>)
                    ) &&
                    (
                        p.PropertyType.GenericTypeArguments[0].GetInterfaces().Contains(typeof(IObjForRegistrar)) ||
                        CustomSerializer.IsSupportedTypeForSerialization(p.PropertyType.GenericTypeArguments[0])
                    )
                select p
            );
            // 2B: Dictionary of <string, supported primitive>, Dictionary of <string, IObjForRegistrar>
            // NOT supported include:
            //     Dictionary with non-string key type,
            //     Dictionary with (non-primitive not implementing IObjForRegistrar) as value
            var listCase2B_DictionaryOfSupportedType =
            (
                from p in propertiesOfCollectionTypes
                where
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                    (
                        p.PropertyType.GenericTypeArguments[0] == typeof(string) &&
                        (
                            p.PropertyType.GenericTypeArguments[1].GetInterfaces().Contains(typeof(IObjForRegistrar)) ||
                            CustomSerializer.IsSupportedTypeForSerialization(p.PropertyType.GenericTypeArguments[1])
                        )
                    )
                select p
            );

            // // Try this block again, once changes in SerializeJustThisObject()
            // // are made so that it will not throw an exception for
            // //     'SortedSet<string> Case2C_OtherCollectionType'
            //var listCase2C_OtherCollectionTypes = 
            //(
            //    // Neither case 2A nor 2B
            //     from p in propertiesOfCollectionTypes
            //     where
            //     (
            //        p.PropertyType.IsGenericType == false ||
            //        (
            //            p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)        == false &&
            //            p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) == false  // This clause is WRONG, does not capture Dictionary<string,Dictionary<string,int>>
            //        )
            //    )
            //    select p
            //);

            var listCase2D_ListOfLIstOfInt =
            (
                from p in propertiesOfCollectionTypes
                where
                    p.PropertyType.IsGenericType &&
                    (
                        p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                        p.PropertyType.GetGenericTypeDefinition() != typeof(Dictionary<,>)
                    ) &&
                    (
                        p.PropertyType.GenericTypeArguments[0].GetInterfaces().Contains(typeof(IObjForRegistrar)) ||
                        CustomSerializer.IsSupportedTypeForSerialization(p.PropertyType.GenericTypeArguments[0])
                    ) == false
                select p
            );

            var listCase2D_DictionaryOfDictionary =
            (
                from p in propertiesOfCollectionTypes
                where
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                    (
                        p.PropertyType.GenericTypeArguments[0] == typeof(string) &&
                        (
                            p.PropertyType.GenericTypeArguments[1].GetInterfaces().Contains(typeof(IObjForRegistrar)) ||
                            CustomSerializer.IsSupportedTypeForSerialization(p.PropertyType.GenericTypeArguments[1])
                        )
                    ) == false
                select p
            );
            #endregion

            int quux = 42;  // breakpoint here
            #endregion

            #region Iterating through sets of same-case properties
            foreach (var p in listCase1A_SupportedPrimitiveTypes)
            {
                // ... inverse of writer.WriteValue(value); as seen in SerializeJustThisObject()
            }

            foreach (var p in listCase1B_IObjForRegistrar)
            {
                // ... tag lookup
                // ... inverse of writer.WriteValue(registeredObj.Tag); as seen in SerializeJustThisObject()
            }

            //foreach (var p in listCase1C_OtherNonCollectionTypes)
            //{
            //    // This category will be ignored for serialization
            //    // (this loop will only exist as a comment, or for debug purposes)
            //}

            foreach (var p in listCase2A_ListOfSupportedType)
            {
                // ... tag lookup if of IObjForRegistrar type
                // ... inverse of CustomSerializer.EmitJsonForList(writer, property, value);
            }

            foreach (var p in listCase2B_DictionaryOfSupportedType)
            {
                // ... tag lookup if of IObjForRegistrar type
                // ... inverse of CustomSerializer.EmitJsonForDictionary(writer, property, value);
            }

            //foreach (var p in listCase2C_OtherCollectionTypes)
            //{
            //    // This category will be ignored for serialization
            //    // (this loop will only exist as a comment, or for debug purposes)
            //}
            //foreach (var p in listCase2D_ListOfLIstOfInt)
            //{
            //    // This category will be ignored for serialization
            //    // (this loop will only exist as a comment, or for debug purposes)
            //}
            //foreach (var p in listCase2D_DictionaryOfDictionary)
            //{
            //    // This category will be ignored for serialization
            //    // (this loop will only exist as a comment, or for debug purposes)
            //}
            #endregion





            // This method returns object?, but this method is typically called by
            // a class-specific wrapper, which will cast the result as SomeParticularType?
            return obj;
        }

        // Algorithm for Deserialization and Tag Lookup:
        // 
        // Consider an object graph, where objects refer to one another via references:
        //     class A {
        //         public B RefToB {get;set;}
        //     }
        //     class B {
        //     }
        // Data for these might be serialized as distinct files
        // (rather than a single blob of JSON for the entire object graph)
        // according to some scheme, such as
        //     (one file per object, or
        //      one file per class, each containing N same-class objects)
        // as in:
        //     A.json
        //     B.json
        // If an instance of (A) is designated as the "root" of an object graph,
        // the resulting graph contains no "cycles", and deserialization
        // (and restoring references from one object to another) is very straightforward,
        // as it suffices to deserialize the classes in reverse order of their dependencies:
        //     deserialize all (B)s
        //     deserialize all (A)s
        // 
        // A more typical situation is of course more complex, as such object graphs
        // are likely to contain references which form cycles.
        // For example:
        //     "Saga" contains "Covenants" and "Characters" and ...
        //     "Covenants" contain "Characters" and "Laboratories" and "Libraries" and ...
        //         also refer to the Saga they are contained in
        //     "Characters" contain "Virtues" and "Flaws" and "Abilities" and "Spells" and other "Characters" and ...
        //        also refer to the Saga, and the Covenant, they are contained in / associated with
        // 
        // Let us therefore consider a simple, abstracted example:
        //     class A {
        //         public B RefToB {get;set;}
        //     }
        //     class B {
        //         public A RefToA {get;set;}
        //     }
        // A scenario of active-at-runtime objects using these classes
        // will form an object graph containing a cycle (A --> B -->A).
        // Upon deserialization, the scheme which worked for the simple "no cycles" case will not suffice,
        // since deserializing any of these classes "first" runs into the problem of handling "forward references":
        //     deserialize all (B)s...what to do with RefToA when constructing the B object?
        //     deserialize all (A)s...what to do with RefToB when constructing the A object?
        // 
        // This is of course not insoluble, being a well-known circumstance admitting various traditional resolutions.
        // The approach we take here is "partial registration".
        // Consider that when deserializing such an object graph, 
        // that there are two cases:
        //     A - Given (some Type T, and JSON for an object of type T -- which may contain references to other types),
        //         construct and return the corresponding object, containing valid object references.
        //         (It is the job of case B to produce a valid reference for any references to other objects.)
        //     B - Given (some Type T, and the string value of the "Tag" for the object thus referenced)
        //         return a valid object reference.
        //         (It is the job of some other code, to eventually deserialize the JSON text
        //         for that object, and fill out the rest of its' data.)
        // 
        // With that preamble, here is the algorithm for deserialization, accomplishing Case A and Case B:
        // 
        // Case A:
        // We define a method
        //     public object? DeserializeFromJson(Type type, string json) { ... }
        // 
        // 1 - Deserialize the JSON to produce a POCO, using a method such as BasicDeserializationToPOCO()
        // 2 - Do Tag Lookup on the "Tag" in the POCO.
        //     2A - Tag Lookup as an ordinary "complete" object;
        //          if found then: What is intended here?  Replace existing object with object from the JSON ?  Throw an exception?
        //     2B - Tag Lookup as a "Partial" object;
        //          the resulting object reference will be used
        //     2C - Tag was not found as Complete or as Partial;
        //          thus a new object will be needed, so call NewObjectFromTypeAndTag(type, tagFromPOCO)
        //     In either of (2B, 2C) the resulting object reference
        //     will be filled out in the remainder of this method.
        // 3 - Fill out properties in the new-or-partial object from step 2.
        //     3A - Property is of supported primitive type: assign the value
        //     3B - Property is of type IObjForRegistrar: Do Tag Lookup to get an object reference,
        //          and call NewObjectFromTypeAndTag() if needed.
        //          It is the job of (this method, via a different call made at a later moment in time) to finish filling that object.
        //     3C - Property is of type List<T>, where T is (supported primitive, or IObjForRegistrar)
        //          Iterate through list elements from the JSON, and
        //          for each element, handle as case 3A or 3B,
        //          adding values to the List in the object being filled out.
        //     3D - Property is of type Dictionary<string,T>, where T is (supported primitive, or IObjForRegistrar)
        //          Iterate through key-value pairs from the JSON, and
        //          for each such pair, handle as case 3A or 3B,
        //          adding key and value to the Dictionary in the object being filled out.
        // 4 - When all properties from the JSON have been gone over
        //     (some may be skipped, due to unsupported type,
        //     or possibly an Attribute indicating to ship it)
        //     then the object is complete, and can be returned.
        //     It is the job of (this method, via a different call made at a later moment in time)
        //     to arrange that all of the objects registered as "Partial" end up completed
        //     (all properties gone over, and re-registered as "complete").
        // 
        //     The code which calls this method is responsible for some arrangement
        //     (such as iterating over all files in the tree-of-dirs-and-files,
        //     possibly doing so in batches of files associated with one Type at a time)
        //     to ensure that all of the objects to deserialize, end up deserialized and registered.
        // 
        // Case B:
        // We define a method
        //     public object? NewObjectFromTypeAndTag(Type type, string Tag) { ... }
        // 
        // 1 - Get the zero-argument constructor for 'type', which is a type implementing IObjForRegistrar
        // 2 - Call that constructor, resulting in an object reference (for an "empty" object)
        // 3 - Set the "Tag"
        // 4 - Register the object in the appropriate ObjRegistrar<T> as a "Partial" object
        // 5 - Return the object reference
        // 
        // At some future moment, it is the job of (other code) to accomplish deserialization of
        // (whatever files) so as to ensure that all "Partial" objects can be
        //     - Filled out with the rest of their data
        //     - Re-registered as ordinary "Complete" objects
        // If that process completes, and there are more than zero "Partial" objects remaining
        // (of whatever Type or Types), then this is an exceptional (and thus Exception-raising)
        // circumstance.  The system could produce an error and discard the incompletely-loaded data,
        // or possibly some manner of error recovery could be attempted.
        // 
        // Upon such error recover, consider:
        //     Suppose there are 30,000 objects in the object graph described
        //     in a "save" (being a tree of dirs-and-files),
        //     and 29,999 of them load correctly, but one remains as a "Partial" object.
        //     This might occur because "FileForTheMissingObject.json" was missing, or malformed.
        //     Though ambitious, one might aspire to have a system which could allow
        //     some means of remedy for this, whether via
        //         (diagnose the problem, and allow the user to manually correct specific files / data contents) or
        //         (doing some "fix-up" on the live data).
        //     This is likely not trivial, so implementation (and designing a working approach) is left as an exercise for the reader...
        #endregion

        #region Previous serialization experiments
        public static void EmitJsonForListViaHardCodedManualApproach(JsonTextWriter writer, object obj)
        {
            // We are given obj, and our caller determined that obj is some manner of List<T>
            // We want to iterate over the elements of that list; the below represents a
            // (hard-coded and inelegant) brute-force approach to doing so.

            writer.WriteStartArray();

            // This requires hard-coding a number of stanzas with known-at-compile-time types.
            // Reflection would be much preferable, but requires considerable wrestling to accomplish with generic types.

            var t1 = obj as List<SimpleArchetypalDataEntity>;
            var t2 = obj as List<SimpleInstanceDataEntity>;
            var t3 = obj as List<int>;
            var t4 = obj as List<string>;
            // ...

            if (t1 != null)
            {
                foreach (var xx in t1)
                {
                    writer.WriteValue(xx.Tag);
                }
            }
            else if (t2 != null)
            {
                foreach (var xx in t2)
                {
                    writer.WriteValue(xx.Tag);
                }
            }
            else if (t3 != null)
            {
                foreach (var xx in t3)
                {
                    writer.WriteValue(xx);
                }
            }
            else if (t4 != null)
            {
                foreach (var xx in t4)
                {
                    writer.WriteValue(xx);
                }
            }
            // ...and so on for various types T which we desire to support in List<T>
            else
            {
                Type type            = obj.GetType();
                var  genericTypeArgs = type.GetGenericArguments();
                Type itemType        = genericTypeArgs[0];

                writer.WriteComment("TODO: Un-handled list type: List<" + itemType.Name + ">");
            }
            writer.WriteEndArray();
        }

        public static void EmitJsonForDictionaryViaHardCodedManualApproach(JsonTextWriter writer, object obj)
        {
            // We are given obj, and our caller determined that obj is some manner of List<T>
            // We want to iterate over the elements of that list; the below represents a
            // (hard-coded and inelegant) brute-force approach to doing so.

            writer.WriteStartObject();

            // This requires hard-coding a number of stanzas with known-at-compile-time types.
            // Reflection would be much preferable, but requires considerable wrestling to accomplish with generic types.

            var t1 = obj as Dictionary<string, SimpleArchetypalDataEntity>;
            var t2 = obj as Dictionary<string, SimpleInstanceDataEntity>;
            var t3 = obj as Dictionary<string, int>;
            var t4 = obj as Dictionary<string, string>;
            // ...and so on for various types T which we desire to support within a Dictionary<SomeKeyType,T>

            if (t1 != null)
            {
                foreach (var kvp in t1)
                {
                    writer.WritePropertyName(kvp.Key);
                    writer.WriteValue(kvp.Value.Tag);
                }
            }
            else if (t2 != null)
            {
                foreach (var kvp in t2)
                {
                    writer.WritePropertyName(kvp.Key);
                    writer.WriteValue(kvp.Value.Tag);
                }
            }
            else if (t3 != null)
            {
                foreach (var kvp in t3)
                {
                    writer.WritePropertyName(kvp.Key);
                    writer.WriteValue(kvp.Value);
                }
            }
            else if (t4 != null)
            {
                foreach (var kvp in t4)
                {
                    writer.WritePropertyName(kvp.Key);
                    writer.WriteValue(kvp.Value);
                }
            }
            // ...and so on for various types T which we desire to support within a Dictionary<SomeKeyType,T>
            else
            {
                Type type            = obj.GetType();
                var  genericTypeArgs = type.GetGenericArguments();
                Type dictKeyType     = genericTypeArgs[0];
                Type dictValueType   = genericTypeArgs[1];

                writer.WriteComment("TODO: Un-handled dictionary type: Dictionary<" + dictKeyType.Name + "," + dictValueType.Name + ">");
            }
            writer.WriteEndObject();
        }
        #endregion
    }

    public class ObjRegistrar<T> where T: IObjForRegistrar
    {
        private Dictionary<Guid,   T> ObjsByGuid = new Dictionary<Guid,   T>();
        private Dictionary<string, T> ObjsByTag  = new Dictionary<string, T>();

        public ObjRegistrar()
        {
            this.ObjsByGuid = new Dictionary<Guid,   T>();
            this.ObjsByTag  = new Dictionary<string, T>();
        }

        public void ClearRegistrar()
        {
            // A method such as this would not be called except in unusual circumstances, such as "reset/clear the Saga"
            this.ObjsByGuid.Clear();
            this.ObjsByTag.Clear();
        }

        public T? LookupObjByGuid(Guid guid)
        {
            bool success = this.ObjsByGuid.TryGetValue(guid, out T? obj);
            if (success) { return obj; }
            return default(T);
        }

        public T? LookupObjByTag(string tag)
        {
            bool success = this.ObjsByTag.TryGetValue(tag, out T? obj);
            if (success) { return obj; }
            return default(T);
        }

        public void RegisterObj(T obj)
        {
            if (obj.Tag == null)
            {
                throw new ArgumentException("Got obj with null Tag");
            }

            #region Already registered?
            var registeredByGuid = this.LookupObjByGuid(obj.Id);
            if (registeredByGuid != null) 
            {
                if (registeredByGuid.Equals(obj)) { return; }  // Already registered, that is OK
                throw new ArgumentException("Got obj already registered for Guid " + obj.Id);
            }
            var registeredByTag  = this.LookupObjByTag(obj.Tag);
            if (registeredByTag != null)
            {
                if (registeredByTag.Equals(obj)) { return; }  // Already registered, that is OK
                throw new ArgumentException("Got obj already registered for Tag " + obj.Tag);
            }
            #endregion

            this.ObjsByGuid[obj.Id] = obj;
            this.ObjsByTag[obj.Tag] = obj;
        }
    }

    public interface IObjForRegistrar
    {
        Guid   Id  { get; set; }
        string Tag { get; set; }
    }
}
