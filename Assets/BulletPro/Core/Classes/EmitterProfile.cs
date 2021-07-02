using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
    // EmissionProfile is the kind of object users will manipulate the most.
    // It stores data of bullets, their patterns, their shots, their bullets, and so on.

    public enum SearchInRecycleBin { Allowed, Only, Exclude }

    [System.Serializable]
    [CreateAssetMenu(fileName = "New Emitter Profile.asset", menuName = "Bullet Pro/Emitter Profile", order = 111)]
    public class EmitterProfile : ScriptableObject
    {
        public BulletParams rootBullet;
        public EmissionParams[] subAssets;

        #if UNITY_EDITOR
        public int buildNumber = 10; // the version of BulletPro this has been created in. Used for automatic updates.
        public bool compactMode; // if true, the sidebar doesn't show up
        public bool hasBeenProcessed; // immediately set to true via AssetPostProcessor. Helps tell CREATED assets from DUPLICATED assets.
        public bool hasBeenInitialized; // immediately set to true in inspector, then creates the whole basic hierarchy.
        public bool displayHelpHierarchy, displayHelpRecycleBin, displayHelpImport;
        public bool foldoutImport, foldoutExport, foldoutEmptyBin, foldoutCompactMode;
        public EmissionParams currentParamsSelected;
        public int numberOfSubAssets; // if not synced with subAssets.arraySize, undoRedoPerformed needs to call AssetDatabase.SaveAssets()
        #endif

        public BulletParams GetBullet(string bulletName, SearchInRecycleBin searchInRecycleBin = SearchInRecycleBin.Allowed)
        {
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (searchInRecycleBin == SearchInRecycleBin.Only && !subAssets[i].isInRecycleBin) continue;
                if (searchInRecycleBin == SearchInRecycleBin.Exclude && subAssets[i].isInRecycleBin) continue;

                if (subAssets[i].name != bulletName) continue;

                if (subAssets[i] is BulletParams)
                    return subAssets[i] as BulletParams;
            }

            return null;
        }

        public ShotParams GetShot(string shotName, SearchInRecycleBin searchInRecycleBin = SearchInRecycleBin.Allowed)
        {
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (searchInRecycleBin == SearchInRecycleBin.Only && !subAssets[i].isInRecycleBin) continue;
                if (searchInRecycleBin == SearchInRecycleBin.Exclude && subAssets[i].isInRecycleBin) continue;

                if (subAssets[i].name != shotName) continue;

                if (subAssets[i] is ShotParams)
                    return subAssets[i] as ShotParams;
            }

            return null;
        }

        public PatternParams GetPattern(string patternName, SearchInRecycleBin searchInRecycleBin = SearchInRecycleBin.Allowed)
        {
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (searchInRecycleBin == SearchInRecycleBin.Only && !subAssets[i].isInRecycleBin) continue;
                if (searchInRecycleBin == SearchInRecycleBin.Exclude && subAssets[i].isInRecycleBin) continue;

                if (subAssets[i].name != patternName) continue;

                if (subAssets[i] is PatternParams)
                    return subAssets[i] as PatternParams;
            }

            return null;
        }

        #if UNITY_EDITOR
        // Called at inspector's OnEnable. Calls SetUniqueIndex() on every subAsset.
        public void SetUniqueIndexes()
        {
            if (subAssets == null) return;
            if (subAssets.Length == 0) return;
            for (int i = 0; i < subAssets.Length; i++)
                subAssets[i].SetUniqueIndex();
        }
        #endif
    }
}
