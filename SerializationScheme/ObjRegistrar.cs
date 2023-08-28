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
    public class ObjRegistrar<T> where T : IObjForRegistrar, new()
    {
        private Dictionary<Guid,   T> ObjsByGuid = new Dictionary<Guid,   T>();
        private Dictionary<string, T> ObjsByTag  = new Dictionary<string, T>();

        // Note:
        // To support handling of "forward references" during deserialization of an object graph,
        // it is useful to be able to register a "Partial" object, generally a newly-constructed and mostly-blank object,
        // which will hopefully be completed during a later point in time.
        // There are two obvious means to implement such a thing:
        //     - Each object implementing IObjForRegistrar
        //       has a boolean member IsComplete (left false by the zero-arg constructor),
        //       which is set to true only when fully filled out
        //     - Objects implementing IObjForRegistrar contain no special members,
        //       rather the ObjRegistrar contains a distinct registration Dictionary for registration of such Partial objects
        // Both of these approaches store data for each Partial object
        // sufficient to determine an answer to the question
        //     "given a Tag not currently registered as a Complete object, does there exist a Partial version?"
        // 
        // The centralized approach is convenient, since it simplifies asking the question
        //     "Are there any Partial objects of type T remaining?" 
        // which one would wish to answer (for each relevant type) at the end of deserializing an object graph.
        // 
        // As such, we define a member PartialObjectsByTag for such a centralized registration dictionary:
        private Dictionary<string, T> PartialObjectsByTag = new Dictionary<string, T>();

        public ObjRegistrar()
        {
            this.ObjsByGuid          = new Dictionary<Guid,   T>();
            this.ObjsByTag           = new Dictionary<string, T>();
            this.PartialObjectsByTag = new Dictionary<string, T>();
        }

        public void ClearRegistrar()
        {
            // A method such as this would not be called except in unusual circumstances, such as "reset/clear the Saga"
            this.ObjsByGuid.Clear();
            this.ObjsByTag.Clear();
            this.PartialObjectsByTag.Clear();
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
            if (obj == null)
            {
                throw new ArgumentException("RegisterObj() called for null");
            }
            if (obj.Tag == null)
            {
                throw new ArgumentException("RegisterObj() Got obj with null Tag");
            }

            #region Already registered?
            var registeredByGuid = this.LookupObjByGuid(obj.Id);
            if (registeredByGuid != null)
            {
                if (registeredByGuid.Equals(obj)) { return; }  // Already registered, that is OK
                throw new ArgumentException("RegisterObj() Got obj already registered for Guid " + obj.Id);
            }
            var registeredByTag = this.LookupObjByTag(obj.Tag);
            if (registeredByTag != null)
            {
                if (registeredByTag.Equals(obj)) { return; }  // Already registered, that is OK
                throw new ArgumentException("RegisterObj() Got obj already registered for Tag " + obj.Tag);
            }
            var registeredAsPartial = this.LookupPartialObjByTag(obj.Tag);
            if (registeredAsPartial != null)
            {
                throw new ArgumentException("RegisterObj() Got obj already registered as Partial for Tag " + obj.Tag);
            }
            #endregion

            this.ObjsByGuid[obj.Id] = obj;
            this.ObjsByTag[obj.Tag] = obj;
        }

        public void UnregisterObj(T obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("UnregisterObj() called for null");
            }
            // Hmmm...should we test and throw exceptions for being called with a non-null but not-registered object?
            // Possibly so...

            this.ObjsByGuid.Remove(obj.Id);
            this.ObjsByTag.Remove(obj.Tag);
            this.PartialObjectsByTag.Remove(obj.Tag);
        }

        public T? LookupPartialObjByTag(string tag)
        {
            bool success = this.PartialObjectsByTag.TryGetValue(tag, out T? obj);
            if (success) { return obj; }
            return default(T);
        }

        public T? RegisterAsPartial(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentException("RegisterAsPartial() Called with null or empty tag");
            }
            var existingObj = this.LookupObjByTag(tag);
            bool alreadyRegistered = (existingObj != null);
            if (alreadyRegistered)
            {
                throw new ArgumentException("RegisterAsPartial() Called for already-registered tag '" + tag + "'.");
            }
            var newObj = new T();
            newObj.Tag = tag;  // The one member we can guarantee to fill out, for now
            this.PartialObjectsByTag[tag] = newObj;
            return newObj;
        }
    }

    public interface IObjForRegistrar
    {
        Guid   Id  { get; set; }
        string Tag { get; set; }
    }
}
