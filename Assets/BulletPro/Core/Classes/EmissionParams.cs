using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
    public enum EmissionParamsType { Bullet, Shot, Pattern }

    // The mother class of BulletParams, ShotParams and PatternParams. These items go into EmissionProfile.bulletHierarchy

    [System.Serializable]
    public class EmissionParams : ScriptableObject
    {
        // values set upon creation
        public EmissionParamsType parameterType;
        public int uniqueIndex;
        
        // references set upon creation
        public EmitterProfile profile;
        public EmissionParams parent;
        public EmissionParams[] children;

        // this is out of editor preprocessing because of EmitterProfile's search functions
        public bool isInRecycleBin;
        
        // changes while editing
        #if UNITY_EDITOR
        public int index;
        public bool foldout;

        // Inherited and called by profile upon creation
        public virtual void FirstInitialization()
        {

        }

        // Finds all occurrences of oldChild (located at a certain index) and replaces them with newChild
        public virtual void ReplaceChild(int indexOfOldChild, EmissionParams newChild)
        {

        }

        // Called at first initialization. Randomizes the unique index within the whole int range.
        public void SetUniqueIndex()
        {
            // not only uints aren't supported, but we only need float precision, so 24 bits are enough
            uniqueIndex = Random.Range(0, (1<<24));
        }
        #endif
    }
}