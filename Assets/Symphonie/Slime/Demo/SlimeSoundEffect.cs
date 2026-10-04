using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Symphonie.StoreAssets {
    public class SlimeSoundEffect : MonoBehaviour
    {
        public Transform RootBone;

        public AudioSource LandingSound;
        public float LandingSoundTriggerSpeed = 1;
        public float LandingHeightThreshold = 0.02f;
        public float LandingSoundPitchShift = 0.2f;



        Vector3 PrevRootLocalPos;
        Vector3 RootLocalVelocity;
        bool LandingSoundTriggered = false;

        private void Start() {
            PrevRootLocalPos = RootBone.position - transform.position;
            RootLocalVelocity = Vector3.zero;
        }


        public void LateUpdate() {
            Vector3 newRootLocalPos = RootBone.position - transform.position;

            if (!LandingSoundTriggered && newRootLocalPos.y <= LandingHeightThreshold && RootLocalVelocity.y < -LandingSoundTriggerSpeed) {
                float pitchScale = Mathf.Pow(2, Random.Range(-LandingSoundPitchShift, LandingSoundPitchShift));
                if (!LandingSound.isPlaying) {
                    LandingSound.pitch = pitchScale;
                    LandingSound.Play();
                }                
                LandingSoundTriggered = true;
            }
            if (RootLocalVelocity.y > 0) {
                LandingSoundTriggered = false;
            }


            RootLocalVelocity = (newRootLocalPos - PrevRootLocalPos) / Time.deltaTime;
            PrevRootLocalPos = newRootLocalPos;
        }
    }

}
