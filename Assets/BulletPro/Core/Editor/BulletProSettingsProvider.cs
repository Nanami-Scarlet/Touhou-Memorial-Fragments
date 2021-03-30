using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

#if UNITY_2018_3_OR_NEWER
namespace BulletPro.EditorScripts
{
    public class BulletProSettingsProvider : SettingsProvider
    {
        public BulletProSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) {}

        static Editor editor;

        [SettingsProvider]
        static SettingsProvider CreateProvider()
        {
            BulletProSettingsProvider bpsp = new BulletProSettingsProvider("Project/Bullet Pro");
            bpsp.guiHandler = OnProviderGUI;

            return bpsp;
        }

        [SettingsProvider]
        static SettingsProvider CreateProviderForPreferences()
        {
            BulletProSettingsProvider bpsp = new BulletProSettingsProvider("Preferences/Bullet Pro", SettingsScope.User);
            bpsp.guiHandler = OnProviderGUI;

            return bpsp;
        }

        static void OnProviderGUI(string context)
        {
            BulletProSettings bps = Resources.Load("BulletProSettings") as BulletProSettings;
			if (bps == null)
                bps = BulletProAssetCreator.CreateCollisionSettingsAsset(false);

            if (!editor)
                Editor.CreateCachedEditor(bps, null, ref editor);
            editor.OnInspectorGUI();
        }
    }
}
#endif