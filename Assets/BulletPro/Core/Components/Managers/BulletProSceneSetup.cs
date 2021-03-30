using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
    public class BulletProSceneSetup : MonoBehaviour
    {
        public bool makePersistentBetweenScenes = false;

        void Awake()
        {
            if (makePersistentBetweenScenes)
                DontDestroyOnLoad(gameObject);
        }

        // An almost-empty script.
        // Its inspector displays a help message and shows gameplay plane orientation.


        public bool enableGizmo = true;
        public Color gizmoColor = Color.white;

        void OnDrawGizmos()
        {
            if (!enableGizmo) return;

            Matrix4x4 oldmat = Gizmos.matrix;

            float gizmoSize = 2f;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Vector3 cubeSize = new Vector3(gizmoSize, gizmoSize, gizmoSize*0.05f);
            Gizmos.DrawCube(transform.position + cubeSize * 0.5f, cubeSize);

            Gizmos.matrix = oldmat;
        }
    }
}
