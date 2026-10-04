using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Symphonie.StoreAssets
{
    [RequireComponent(typeof(Animator))]
    public class IgnoreRootMotion : MonoBehaviour
    {
        public string PlayOnStart = "Walk";

        public float RecoverSpeed = 1;
        public float MaxDistance = 4;

        Animator Animator;
        Vector3 InitPos;
        public Vector3 Velocity;

        float VelDeri = 0;

        private void Awake() {
            Animator = GetComponent<Animator>();
        }

        private void Start()
        {
            InitPos = transform.position;

            if (Animator != null) {
                Animator.PlayInFixedTime(PlayOnStart, 0);
            }
        }

        static Vector3 Flatten(Vector3 v) {
            v.y = 0;
            return v;
        }

        private void OnAnimatorMove()
        {
            float newSpeed = Mathf.SmoothDamp(Velocity.magnitude, Animator.deltaPosition.magnitude / Time.deltaTime, ref VelDeri, 6);
            Velocity = Animator.deltaPosition.normalized * newSpeed;

            transform.rotation *= Animator.deltaRotation;

            Vector3 recoverVel = -Velocity + (InitPos - transform.position) * RecoverSpeed;


            transform.position += Flatten(recoverVel) * Time.deltaTime + Animator.deltaPosition;
            transform.position = Vector3.ClampMagnitude(transform.position - InitPos, MaxDistance) + InitPos;

            
        }
    }

}