using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace Symphonie.StoreAssets
{
    public class SlimeController : MonoBehaviour
    {
        public float MaxRotateSpeed = 200.0f;
        public float Gravity = 5;

        public bool AIControlled = false;
        public float AIMinRetargetInterval = 2.0f;
        public float AIMaxRetargetInterval = 5.0f;

        public Transform FollowTarget;
        public float FollowDistance = 2;
        public float FollowRepathTime = 0.5f;



        NavMeshAgent Agent;
        Animator Animator;
        CharacterController Controller;

        Vector3 PrevPos;
        Vector3 Velocity;


        static Vector3 Flatten(Vector3 v3) {
            v3.y = 0;
            return v3;
        }


        private void Awake() {
            Agent = GetComponentInChildren<NavMeshAgent>();
            Agent.updatePosition = false;
            Agent.updateRotation = false;
                      
            Animator = GetComponentInChildren<Animator>();
            //Animator.applyRootMotion = false;
            Animator.SetFloat("ZSpeed", 0);
            Animator.SetFloat("XSpeed", 0);

            Controller = GetComponentInChildren<CharacterController>();
        }


        private void Start() {
            PrevPos = transform.position;
            Velocity = Vector3.zero;



            if (AIControlled) {
                if (FollowTarget != null)
                    StartCoroutine(AI_Follow());
                StartCoroutine(AI_RandomMove());
            }
        }

        IEnumerator AI_RandomMove() {
            float maxDist = 8;
            while (true) {
                // try to find a valid destination, else wait for next time
                for (int i = 0; i < 3; ++i) {
                    if (NavMesh.SamplePosition(transform.position + Flatten(Random.onUnitSphere).normalized * maxDist, out var hit, maxDist / 2, -1)) {
                        Agent.destination = hit.position;
                        break;
                    }
                }

                yield return new WaitForSeconds(Random.Range(AIMinRetargetInterval, AIMaxRetargetInterval));
            }
        }
        IEnumerator AI_Follow() {
            // prevent re-path at the same time
            yield return new WaitForSeconds(Random.value * 2);

            while (true) {
                // try to find a valid destination, else wait for next time
                for (int i = 0; i < 3; ++i) {
                    if (NavMesh.SamplePosition(FollowTarget.position, out var hit, 4, -1)) {
                        if (Vector3.Distance(hit.position, transform.position) < FollowDistance) {
                            Agent.ResetPath();
                            Agent.isStopped = true;                            
                        }
                        else {
                            var dest = Vector3.ClampMagnitude(transform.position - hit.position, FollowDistance) + hit.position;
                            Agent.isStopped = false;
                            Agent.destination = dest;
                        }                        
                        break;
                    }
                }

                // 5% chance to take a nap
                float waitTime = FollowRepathTime;
                if (Random.value < 0.05f) {
                    waitTime = 10;
                    Agent.ResetPath();
                }
                    
                yield return new WaitForSeconds(FollowRepathTime);
            }
        }


        private void Update() {
            float dspeed = Agent.desiredVelocity.magnitude;

            float runDist = 4;

            if (dspeed > 0.1f && !Agent.isStopped) {
                float angle = Vector3.Angle(Flatten(transform.forward), Flatten(Agent.desiredVelocity));
                if (angle > 30) {
                    SetZSpeed(0);
                }
                else {
                    float d = Mathf.Max(Vector3.Distance(transform.position, Agent.steeringTarget),
                        Vector3.Distance(transform.position, Agent.destination));
                    float s = 1;
                    if (d > runDist)
                        s = Mathf.Lerp(2, 4, Mathf.Clamp01((d - runDist) / 16));
                    else
                        s = d / runDist * 3;
                    SetZSpeed(s);
                }
            }
            else {
                SetZSpeed(0);
            }

            Quaternion rot = Quaternion.RotateTowards(
                transform.rotation,
                transform.rotation * Quaternion.FromToRotation(transform.forward, Flatten(Agent.desiredVelocity.normalized)),
                MaxRotateSpeed * Time.deltaTime);
            transform.rotation = rot.normalized;

            Debug.DrawLine(transform.position, transform.position + transform.forward, Color.yellow);
            if (!Agent.isStopped)
                Debug.DrawLine(transform.position, Agent.destination, Color.blue);
            Agent.nextPosition = transform.position;
                        
        }

        private void LateUpdate() {
            Velocity.y -= Gravity * Time.deltaTime;

            if (!Controller.isGrounded)
                Controller.SimpleMove(new Vector3(0, Velocity.y, 0));

            Velocity = (transform.position - PrevPos) / Time.deltaTime;
            PrevPos = transform.position;
        }

        void SetZSpeed(float speed) {
            float current = Animator.GetFloat("ZSpeed");
            if (current > speed)
                Animator.SetFloat("ZSpeed", speed, 0.5f, Time.deltaTime);
            else
                Animator.SetFloat("ZSpeed", speed, 0.2f, Time.deltaTime);
        }


        private void OnGUI() {
            if (AIControlled)
                return;

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && Agent != null) {
                var screenPoint = evt.mousePosition;
                screenPoint.y = Screen.height - screenPoint.y;
                var mouseRay = Camera.main.ScreenPointToRay(screenPoint);
                if (Physics.Raycast(mouseRay, out var hitInfo)) {
                    Agent.destination = hitInfo.point;
                    Debug.DrawLine(transform.position, hitInfo.point, Color.red, 0.5f);
                    evt.Use();
                }
            }
            
        }

    }

}
