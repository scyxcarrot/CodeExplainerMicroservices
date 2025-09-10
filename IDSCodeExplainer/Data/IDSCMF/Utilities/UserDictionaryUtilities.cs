using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;

namespace IDS.CMF.Utilities
{
    public static class UserDictionaryUtilities
    {
        /// <summary>
        /// This method changes the user dictionary of rhino object such that it supports undo redo
        /// Can also use this to support adding User Dictionary
        /// </summary>
        /// <param name="doc">Current rhino document that has the rhino object</param>
        /// <param name="rhinoObj">rhino object where you want to change the user dictionary</param>
        /// <param name="userDictionaryKey">Key of the new user dictionary</param>
        /// <param name="userDictionaryValue">Value of the new user dictionary</param>
        /// <returns>boolean saying that the modification was successful (true) or failed (false)</returns>
        public static bool ModifyUserDictionary(RhinoObject rhinoObj, string userDictionaryKey, object userDictionaryValue)
        {
            var doc = rhinoObj.Document;
            var modifiedObjectAttributes = rhinoObj.Attributes.Duplicate();
            SetModifiedContentInDictionary(rhinoObj, userDictionaryKey, userDictionaryValue,
                out var modifiedUserDictionaries);

            modifiedObjectAttributes.UserDictionary.AddContentsFrom(modifiedUserDictionaries);
            if (doc == null)
            {
                return rhinoObj.Attributes.UserDictionary.AddContentsFrom(modifiedUserDictionaries);
            }

            return doc.Objects.ModifyAttributes(rhinoObj, modifiedObjectAttributes, true);
        }

        public static bool ReplaceContentDictionaryUnsafe(RhinoObject rhinoObj, string userDictionaryKey,
            object userDictionaryValue)
        {
            // Please be aware that ReplaceContentsWith might not work as expected for Undo/Redo if an object is not changed in the doc.
            // BUG 1190141: ModifyAttributes might cause out of sync issue, using ReplaceContentWith would avoid this issue

            var doc = rhinoObj.Document;
            var modifiedObjectAttributes = rhinoObj.Attributes.Duplicate();
            SetModifiedContentInDictionary(rhinoObj, userDictionaryKey, userDictionaryValue,
                out var modifiedUserDictionaries);

            modifiedObjectAttributes.UserDictionary.AddContentsFrom(modifiedUserDictionaries);
            if (doc == null)
            {
                return rhinoObj.Attributes.UserDictionary.AddContentsFrom(modifiedUserDictionaries);
            }

            var replacedContent = rhinoObj.Attributes.UserDictionary.ReplaceContentsWith(modifiedUserDictionaries);
            return replacedContent && rhinoObj.CommitChanges();
        }

        private static void SetModifiedContentInDictionary(RhinoObject rhinoObj, string userDictionaryKey, object userDictionaryValue, out ArchivableDictionary modifiedUserDictionaries)
        {
            modifiedUserDictionaries = rhinoObj.Attributes.UserDictionary.Clone();
            var userDictionaryValueType = userDictionaryValue.GetType();

            #region userDictValueTypeIfBlock
            if (userDictionaryValueType == typeof(string))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (string)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(bool))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (bool)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(int))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (int)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(double))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (double)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(Guid))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (Guid)userDictionaryValue);
            }
            else if (typeof(IEnumerable<bool>).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (IEnumerable<bool>)userDictionaryValue);
            }
            else if (typeof(IEnumerable<int>).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (IEnumerable<int>)userDictionaryValue);
            }
            else if (typeof(IEnumerable<double>).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (IEnumerable<double>)userDictionaryValue);
            }
            else if (typeof(IEnumerable<Guid>).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (IEnumerable<Guid>)userDictionaryValue);
            }
            else if (typeof(IEnumerable<string>).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (IEnumerable<string>)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(Color))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (Color)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(Point3d))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (Point3d)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(Vector3d))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (Vector3d)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(Transform))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (Transform)userDictionaryValue);
            }
            else if (userDictionaryValueType == typeof(MeshingParameters))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (MeshingParameters)userDictionaryValue);
            }
            else if (typeof(GeometryBase).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (GeometryBase)userDictionaryValue);
            }
            else if (typeof(ObjRef).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (ObjRef)userDictionaryValue);
            }
            else if (typeof(IEnumerable<ObjRef>).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (IEnumerable<ObjRef>)userDictionaryValue);
            }
            else if (typeof(IEnumerable<GeometryBase>).IsAssignableFrom(userDictionaryValueType))
            {
                modifiedUserDictionaries.Set(userDictionaryKey, (IEnumerable<GeometryBase>)userDictionaryValue);
            }
            else
            {
                throw new ArgumentException("Please update the code to accept the new userDictionaryValue type");
            }
            #endregion
        }
    }
}