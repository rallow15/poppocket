using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Symphonie.StoreAssets {
    [RequireComponent(typeof(Camera))]
    public class SimpleCameraFollow : MonoBehaviour {
        public Transform Target;

        public float HorizontalDampTime = 0.2f;
        public float VerticalDampTime = 0.5f;



        Transform CameraTransform;
        Vector3 CameraVelocity = Vector3.zero;
        float VertVelocity = 0;
        Vector3 CameraOffset = Vector3.zero;


        private void Awake() {
            CameraTransform = GetComponent<Camera>()?.transform;
        }

        private void Start() {
            CameraOffset = CameraTransform.position - Target.position;
        }

        private void LateUpdate() {
            Vector3 newHoriPos = Vector3.SmoothDamp(CameraTransform.position, Target.position + CameraOffset, ref CameraVelocity, HorizontalDampTime);
            newHoriPos.y = Mathf.SmoothDamp(CameraTransform.position.y, (Target.position + CameraOffset).y, ref VertVelocity, VerticalDampTime);

            CameraTransform.position = newHoriPos;
        }
    }

}
