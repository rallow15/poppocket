using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Symphonie.StoreAssets
{
    [ExecuteInEditMode]
    public class SlimeVisual : MonoBehaviour {
        [Header("Color")]
        public bool OverrideColor = false;
        //[ColorUsage(false, true)]
        public Color SlimeColor = new Color(0.5f, 0.6f, 1.0f);

        [Header("Core")]
        [Tooltip("The transform of the core of the slime. Its position will be sync to material using MaterialPropertyBlock in order to rendering the core correctly.")]
        public Transform CoreTransform;


        [Header("Dynamics")]
        public bool EnableDynamics = true;
        [Tooltip("Provides the position which the core returns to")]
        public Transform CenterTransform;
        public Vector3 CenterOffset;
        public Vector3 HardDistance = new Vector3(0.3f, 0.2f, 0.3f);
        public Vector3 SoftDistance = new Vector3(0.05f, 0.04f, 0.05f);
        public float SpringForce = 1000;
        public float Damping = 1.0f;
		[Range(0, 1)]
		public float InertiaScale = 0.6f;

        Renderer[] SlimeRenderers;
        MaterialPropertyBlock MtlPropBlock;




        static Transform FindInChildren(Transform root, string name) {
            if (root == null)
                return null;
            if (root.gameObject.name == name)
                return root;
            for (int i = 0; i < root.childCount; ++i) {
                var t = FindInChildren(root.GetChild(i), name);
                if (t != null)
                    return t;
            }
            return null;
        }
        static Vector3 Div(Vector3 a, Vector3 b) {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
        static Vector3 Mul(Vector3 a, Vector3 b) {
            a.Scale(b);
            return a;
        }



        private void Awake() {
            if (CenterTransform == null)
                CenterTransform = FindInChildren(transform, "Anchor_Center");
            
            if (CoreTransform == null)
                CoreTransform = FindInChildren(transform, "Core");
        }


        private void Start() {
            SlimeRenderers = GetComponentsInChildren<Renderer>();

            PrevCenterPos = Pos = PrevPos = CoreTransform.position;
        }


        private void LateUpdate()
        {            
            if (Application.isPlaying && EnableDynamics)
                Simulate();

            if (MtlPropBlock == null) {
                MtlPropBlock = new MaterialPropertyBlock();
            }

            foreach (var renderer in SlimeRenderers) {
                renderer.GetPropertyBlock(MtlPropBlock);

                if(OverrideColor) {
                    MtlPropBlock.SetVector("_Color", SlimeColor);                            
                }
                
                if (CoreTransform != null) {
                    MtlPropBlock.SetVector("_CorePosition", CoreTransform.position);
                }

                renderer.SetPropertyBlock(MtlPropBlock);
            }
                
            
        }


        Vector3 PrevPos;
        Vector3 Pos;
        Vector3 PrevCenterPos;


        void Simulate() {

            float dt = Mathf.Clamp(Time.smoothDeltaTime, 1.0f / 200.0f, 1.0f / 10.0f);


            // center pos
            Vector3 centerPos = CenterTransform.TransformPoint(CenterOffset);
            Vector3 hardDist = HardDistance;
            Vector3 softDist = SoftDistance;
			
			// inertia scaling
			Vector3 inertiaDelta = (centerPos - PrevCenterPos) * Mathf.Clamp01(1 - InertiaScale);
			Pos += inertiaDelta;
			PrevPos += inertiaDelta;

            // accel
            Vector3 accel = Vector3.zero;
            Vector3 targetPos = Mul(Vector3.ClampMagnitude(Div(Pos - centerPos, softDist), 1), softDist) + centerPos;
            accel = (targetPos - Pos) * SpringForce;


            // integration
            float dampingScale = Mathf.Pow(Mathf.Max(0, 1 - Damping / 60), dt * 60);

            Vector3 vel = (Pos - PrevPos) / dt;
            Vector3 anchorVel = (centerPos - PrevCenterPos) / dt;
            vel = (vel - anchorVel) * dampingScale + anchorVel;

            Pos = Pos + vel * dt + 0.5f * accel * dt * dt;


            // constraint            
            Pos = Mul(Vector3.ClampMagnitude(Div(Pos - centerPos, hardDist), 1), hardDist) + centerPos;


            PrevPos = Pos;
            PrevCenterPos = centerPos;


            CoreTransform.position = Pos;
        }

    }
}
