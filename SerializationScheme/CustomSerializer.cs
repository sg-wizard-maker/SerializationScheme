using Microsoft.VisualBasic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SerializationScheme
{
    public class CustomSerializer
    {
        public static void SerializeJustThisObject(object obj)
        {
            var sw = new StringWriter();
            var writer = new JsonTextWriter(sw);

            writer.WriteStartObject();

            Type           thisObjType = obj.GetType();
            BindingFlags   flags       = (BindingFlags.Public | BindingFlags.Instance);
            //FieldInfo[]  fields      = type.GetFields(flags);  // Using only Properties, rather than a mix of Fields and Properties
            PropertyInfo[] properties  = thisObjType.GetProperties(flags);

            #region Handle Public Properties (only)
            // TODO: May want a mechanism (such as attribute [JsonIgnore]) to skip some members
            // TODO: May want ability to specify that (some field) or (some private property) should be emitted
            //       Then again, perhaps the simplest convention is "public properties only"...
            for (int ii = 0; ii < properties.Length; ii++)
            {
                var property     = properties[ii];
                var propertyName = property.Name;
                var value        = property.GetValue(obj);

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
                    CustomSerializer.EmitJsonForListViaHardCodedManualApproach(writer, value);
                    //CustomSerializer.EmitJsonForListViaReflection(writer, value);
                    continue;
                }
                // Case 3: Value is some kind of Dictionary<SomeKeyType,T>
                if (typeOfPropertyValue.IsGenericType && typeOfPropertyValue.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    CustomSerializer.EmitJsonForDictionaryViaHardCodedManualApproach(writer, value);
                    //CustomSerializer.EmitJsonForDictionaryViaReflection(writer, value);
                    continue;
                }

                // Case 4: Value is a primitive type such as (int, string)
                IObjForRegistrar? registeredObj = value as IObjForRegistrar;
                if (registeredObj == null)
                {
                    // Usually one of the fundamental types;
                    // encountering SomeRandomClass would cause a JsonWriterException here
                    writer.WriteValue(value);
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
        }

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

        public static void EmitJsonForListViaReflection(JsonTextWriter writer, object obj)
        {
            Type type = obj.GetType();

            var  genericTypeDefinition = type.GetGenericTypeDefinition();
            var  genericTypeArgs       = type.GetGenericArguments();
            Type itemType              = genericTypeArgs[0];

            // Nothing successful thus far, but should be possible via Reflection with the right incantations...

            //var getEnumeratorMethod = typeDefinition.GetMethod("GetEnumerator");

            // A
            //var methodInfo = getEnumeratorMethod?.MakeGenericMethod(itemType);
            //var xxx = methodInfo?.Invoke(property, null);

            // B
            //var xxx = getEnumeratorMethod?.Invoke(value, null);

            // (Other)
            //var resultofGetEnumerator = typeDefinition.InvokeMember("GetEnumerator", BindingFlags.Instance | BindingFlags.InvokeMethod, null, property, null);

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

        public static void EmitJsonForDictionaryViaReflection(JsonTextWriter writer, object obj)
        {
            Type type                  = obj.GetType();
            var  genericTypeDefinition = type.GetGenericTypeDefinition();
            var  genericTypeArgs       = type.GetGenericArguments();
            Type dictKeyType           = genericTypeArgs[0];
            Type dictValueType         = genericTypeArgs[1];

            // ...TBD
        }

        public static void ReflectionUponListType(object obj)
        {
            // Experimenting upon how to use Reflection to look inside of a List<T> when you have an 'object' in hand...
            Type objType = obj.GetType();

            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var  typeDefinition  = objType.GetGenericTypeDefinition();
                var  genericTypeArgs = objType.GetGenericArguments();
                Type itemType        = genericTypeArgs[0];

                // Given (obj, objType, typeDefinition, itemType),
                // and knowing that this method was called with 'obj' being a List<int>,
                // we would like to get a hold of 'obj' in terms of List<int>, rather than 'object',
                // so as to be able to access the enumerator, and use foreach()



                //foreach (var xx in XXXXX)
                //{
                //}

            }
            else
            {
                // ...
            }
        }

        //public static Type GetAnyElementType(Type type)
        //{
        //    // Type is Array
        //    // short-circuit if you expect lots of arrays 
        //    if (type.IsArray)
        //    {
        //        var arrayResult = type.GetElementType();
        //        return arrayResult;
        //    }
        //
        //    // Type is IEnumerable<T>;
        //    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        //    {
        //        var iEnumerableResult = type.GetGenericArguments()[0];
        //        return iEnumerableResult;
        //    }
        //
        //    // Type implements/extends IEnumerable<T>;
        //    var enumType = type
        //        .GetInterfaces()
        //        .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        //        .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
        //
        //    var result = enumType ?? type;
        //    return result;
        //}
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
