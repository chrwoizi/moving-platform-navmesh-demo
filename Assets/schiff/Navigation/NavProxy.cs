
using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.schiff
{
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(NavSpace))]
    public class NavProxy : MonoBehaviour
    {
        public Transform Target;
        public Transform TargetModel;
        public Animator Animator;

        private NavMeshAgent agent;
        private NavSpace space;
        private bool isInitialized;

        private Action<bool> onDone;
        private Vector3 startLocation;
        private Vector3 destinationLocation;
        private Vector3 currentProxyDestination;

        private bool isNavigating;
        private float startRotation;
        private Vector3 lastTargetModelPosition;
        private Vector3 lastForward;
        private bool isMoving;
        private bool result;
        private Vector3? offMeshStart;
        private float offMeshSpeed;
        private float offMeshLength;
        private float walkedDistance;
        private Vector3? offMeshStartPoint;
        private Vector3? offMeshEndPoint;
        private Vector3 velocity = Vector3.zero;
        private Vector3 lastPosition;
        private GameObject fromVis;
        private GameObject toVis;

        /// <summary>
        /// The minimum of the total walked distance on all paths and the remaining distance of the current path
        /// </summary>
        private float movingDirectionFactorDistance;

        /// <summary>
        /// How many units are used to rotate the character to the walk direction and back
        /// </summary>
        public float RotationFactor = 1;

        /// <summary>
        /// How many units are used to lift the character to the link height and back
        /// </summary>
        public float HeightFactor = 0.2f;

        /// <summary>
        /// Animator turning sensitivity
        /// </summary>
        public float TurnSensitivity = 0.2f;

        /// <summary>
        /// Animator turning interpolation speed
        /// </summary>
        public float TurnSpeed = 5f;

        public float NavMeshSearchDistance = 2;

        public float SlowDownFromAngle = 180;

        public GameObject VisPrefab;

        public bool IsNavigating
        {
            get
            {
                return isNavigating;
            }
        }

        public NavMeshAgent Agent
        {
            get
            {
                return agent;
            }
        }

        public bool IsOnOffmeshLink
        {
            get
            {
                return isInitialized && agent.isOnOffMeshLink;
            }
        }

        public float? DestinationRotation;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            space = GetComponent<NavSpace>();
        }

        void OnDestroy()
        {
            if (onDone != null)
            {
                onDone(result);
            }
        }

        public Vector3 GetCurrentModelDestination()
        {
            return space.ToPlatform(currentProxyDestination);
        }

        public void Initialize()
        {
            isInitialized = true;

            agent.enabled = true;

            transform.rotation = Quaternion.Euler(0, GetTargetModelLocalRotation().eulerAngles.y, 0);

            var position = space.ToProxy(GetTargetModelPosition());

            NavMeshHit hit;
            NavMesh.SamplePosition(position, out hit, NavMeshSearchDistance, agent.areaMask);
            if (!hit.hit)
            {
                Debug.LogError("Cannot initialize NavProxy at " + GetTargetModelPosition() + " (proxy at " + position + ")");
                return;
            }

            agent.Warp(hit.position);
        }

        public virtual void Navigate(Vector3 modelDestination, Action<bool> onDone)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (IsNavigating)
            {
                Debug.Log("Already navigating");
                Stop();
            }

            this.isNavigating = true;
            this.onDone = onDone;

            isMoving = false;
            result = false;

            var from = space.ToProxy(GetTargetModelPosition());
            var to = space.ToProxy(modelDestination);
            currentProxyDestination = to;

            NavMeshHit fromHit;
            NavMesh.SamplePosition(from, out fromHit, NavMeshSearchDistance, agent.areaMask);
            if (!fromHit.hit)
            {
                Debug.Log("Cannot navigate from " + GetTargetModelPosition() + " (proxy at " + from + ")");
                Stop();
                return;
            }

            NavMeshHit toHit;
            NavMesh.SamplePosition(to, out toHit, NavMeshSearchDistance, agent.areaMask);
            if (!toHit.hit)
            {
                Debug.Log("Cannot navigate to " + modelDestination + " (proxy at " + to + ")");
                Stop();
                return;
            }

            if (VisPrefab != null)
            {
                fromVis = Instantiate(VisPrefab, space.ToPlatform(from), Quaternion.identity);
                toVis = Instantiate(VisPrefab, space.ToPlatform(to), Quaternion.identity);
            }

            this.startLocation = fromHit.position;
            this.destinationLocation = toHit.position;

            Animator.SetBool("Crouch", false);
            Animator.SetBool("OnGround", true);
            Animator.SetBool("IsStrafing", true);
        }

        public void Stop()
        {
            if (!isNavigating)
            {
                return;
            }

            isNavigating = false;
            isMoving = false;
            result = false;

            Rotate(0, Vector3.zero, 0);

            Animator.SetFloat("Turn", 0);
            Animator.SetFloat("Forward", 0);
            Animator.SetFloat("Right", 0);
            
            currentProxyDestination = Vector3.zero;

            if (fromVis != null)
            {
                Destroy(fromVis);
                fromVis = null;
            }

            if (toVis != null)
            {
                Destroy(toVis);
                toVis = null;
            }

            startLocation = Vector3.zero;
            destinationLocation = Vector3.zero;

            Animator.SetBool("Crouch", false);
            Animator.SetBool("OnGround", true);
            Animator.SetBool("IsStrafing", true);

            OnEndMovement();
        }

        void Update()
        {
            if (!isNavigating)
            {
                return;
            }

            if (!isMoving)
            {
                var currentPos = GetTargetModelPosition();
                var startPos = space.ToPlatform(startLocation);
                var toStart = startPos - currentPos;
                var dist = toStart.magnitude;
                if (dist > 0.01f)
                {
                    var speed = Mathf.Min(agent.speed, velocity.magnitude + Time.deltaTime * agent.acceleration);
                    velocity = toStart / dist * speed;
                    var modelPosition = Vector3.MoveTowards(currentPos, startPos, Time.deltaTime * speed);
                    Target.position = modelPosition - Target.TransformVector(GetTargetModelLocalPosition());
                }
                else
                {
                    if (!agent.Warp(startLocation))
                    {
                        result = false;
                        Stop();
                        return;
                    }

                    agent.destination = destinationLocation;
                    agent.velocity = velocity;
                    startRotation = TargetModel.rotation.eulerAngles.y;
                    lastTargetModelPosition = GetTargetModelPosition();
                    lastForward = TargetModel.forward;
                    walkedDistance = 0;
                    offMeshStart = null;
                    isMoving = true;
                    lastPosition = transform.position;
                }
            }
            else
            {
                Vector3 modelPosition;

                if (agent.isOnOffMeshLink)
                {
                    if (agent.currentOffMeshLinkData.valid && agent.path.corners.Any() && agent.path.corners[0] == agent.currentOffMeshLinkData.endPos)
                    {
                        if (offMeshStart == null)
                        {
                            InitOffMeshMovement();
                        }

                        var endPos = agent.currentOffMeshLinkData.endPos;

                        MoveOffMesh();

                        var factor = Vector3.Distance(transform.position, offMeshStart.Value) / offMeshLength;
                        var a = space.ToPlatform(transform.position, endPos);
                        var b = space.ToPlatform(transform.position, offMeshStart.Value);

                        var interpPosition = Vector3.Lerp(b, a, factor);

                        modelPosition = interpPosition;
                    }
                    else
                    {
                        // off-mesh link was destroyed

                        if (offMeshStartPoint != null)
                        {
                            // go to off-mesh start
                            transform.position = offMeshStartPoint.Value;
                            Target.position = space.ToPlatform(transform.position) - Target.TransformVector(GetTargetModelLocalPosition());
                        }

                        agent.CompleteOffMeshLink();

                        Stop();
                        return;
                    }
                }
                else
                {
                    offMeshStart = null;
                    modelPosition = space.ToPlatform(transform.position);
                }

                velocity = transform.position - lastPosition;
                lastPosition = transform.position;

                Target.position = modelPosition - Target.TransformVector(GetTargetModelLocalPosition());

                var currentPosition = GetTargetModelPosition();
                var movedDelta = currentPosition - lastTargetModelPosition;
                lastTargetModelPosition = currentPosition;

                var movedDistance = movedDelta.magnitude;
                walkedDistance += movedDistance;

                var remainingLinearDistance = Vector3.Distance(transform.position, destinationLocation);

                Rotate(remainingLinearDistance, movedDelta, movedDistance);

                if (agent.remainingDistance <= 0)
                {
                    EndMovement();
                }
                else
                {
                    UpdateAnimation(movedDelta);
                }
            }
        }

        private Quaternion GetTargetModelLocalRotation()
        {
            return Quaternion.Inverse(Target.rotation) * TargetModel.rotation;
        }

        private Vector3 GetTargetModelLocalPosition()
        {
            return Target.InverseTransformPoint(GetTargetModelPosition());
        }

        private Vector3 GetTargetModelPosition()
        {
            var v = TargetModel.position;
            v.y = Target.position.y;
            return v;
        }

        private void Rotate(float remainingLinearDistance, Vector3 movedDelta, float movedDistance)
        {
            movingDirectionFactorDistance = Math.Min(movingDirectionFactorDistance + movedDistance, remainingLinearDistance);

            var walkingFactor = Mathf.Clamp01(movingDirectionFactorDistance / RotationFactor);

            var standingRotation = GetStandingRotation(remainingLinearDistance);
            var walkingRotation = transform.rotation.eulerAngles.y;
            var rotation = Mathf.LerpAngle(standingRotation, walkingRotation, walkingFactor);

            var center = GetTargetModelPosition();

            var delta = rotation - TargetModel.rotation.eulerAngles.y;
            if (delta != 0)
            {
                Target.RotateAround(center, Vector3.up, delta);
            }

            OnMove(walkingFactor, movedDelta, delta, center);
        }

        protected virtual float GetStandingRotation(float remainingLinearDistance)
        {
            float targetRotation;
            if (walkedDistance < remainingLinearDistance)
            {
                targetRotation = startRotation;
            }
            else
            {
                targetRotation = GetDestinationRotation();
            }

            return targetRotation;
        }

        protected virtual float GetDestinationRotation()
        {
            return DestinationRotation.HasValue ? DestinationRotation.Value : transform.rotation.eulerAngles.y;
        }

        protected virtual void OnMove(float walkingFactor, Vector3 movedDelta, float delta, Vector3 center)
        {
        }

        private void UpdateAnimation(Vector3 movedDelta)
        {
            float angle = -GetAngleFromForward(TargetModel, lastForward);
            lastForward = TargetModel.forward;
            angle *= TurnSensitivity * 0.01f;
            angle = Mathf.Clamp(angle / Time.deltaTime, -1f, 1f);

            Animator.SetFloat("Turn", Mathf.Lerp(Animator.GetFloat("Turn"), angle, Time.deltaTime * TurnSpeed));

            var velocity = TargetModel.InverseTransformDirection(movedDelta) / Time.deltaTime;
            Animator.SetFloat("Forward", velocity.z);
            Animator.SetFloat("Right", velocity.x);
        }

        private void EndMovement()
        {
            isNavigating = false;

            Rotate(0, Vector3.zero, 0);

            Animator.SetFloat("Turn", 0);
            Animator.SetFloat("Forward", 0);
            Animator.SetFloat("Right", 0);

            OnEndMovement();

            result = true;
            
            if (onDone != null)
            {
                onDone(result);
            }
        }

        protected virtual void OnEndMovement()
        {
        }

        private void MoveOffMesh()
        {
            var toEnd = agent.currentOffMeshLinkData.endPos - transform.position;
            toEnd.y = 0;

            var distToEnd = toEnd.magnitude;

            if (distToEnd > 0.01f)
            {
                var toEndNormalized = toEnd / distToEnd;
                var target2d = new Vector3(toEndNormalized.x, 0, toEndNormalized.z);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target2d.normalized), Time.deltaTime * offMeshSpeed * agent.angularSpeed);

                var angle = Vector3.Angle(toEndNormalized, transform.forward);
                var slowDown = angle > 0 ? 1 - Mathf.Clamp01(angle / SlowDownFromAngle) : 1;

                var speed = agent.speed * offMeshSpeed * slowDown;
                var movement = Time.deltaTime * speed;

                transform.position += movement * transform.forward;

                if (offMeshStartPoint.HasValue && offMeshEndPoint.HasValue)
                {
                    var fraction = distToEnd / offMeshLength;
                    var height = Mathf.Lerp(offMeshEndPoint.Value.y, offMeshStartPoint.Value.y, fraction);
                    var targetHeightFactor = Mathf.Clamp01(Mathf.Min(distToEnd, offMeshLength - distToEnd) / HeightFactor);
                    var y = Mathf.Lerp(agent.currentOffMeshLinkData.startPos.y, height, targetHeightFactor);
                    transform.position = new Vector3(transform.position.x, y, transform.position.z);//Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, y, transform.position.z), Time.deltaTime * speed);
                }

                if (movement >= distToEnd)
                {
                    EndOffMeshMovement();
                }
                else
                {
                    Target.transform.SetParent(null, true);
                }
            }
            else
            {
                EndOffMeshMovement();
            }
        }

        private void EndOffMeshMovement()
        {
            transform.position = new Vector3(transform.position.x, agent.currentOffMeshLinkData.endPos.y, transform.position.z);

            if (space.IsMultiPlatform)
            {
                Target.SetParent(agent.currentOffMeshLinkData.endPos.x < 0 ? space.platformB : space.platformA);
            }

            agent.CompleteOffMeshLink();
            OnEndOffMeshMovement();
        }

        protected virtual void OnEndOffMeshMovement()
        {
        }

        private void InitOffMeshMovement()
        {
            offMeshStart = transform.position;
            offMeshLength = Vector3.Distance(agent.currentOffMeshLinkData.endPos, offMeshStart.Value);
            offMeshSpeed = offMeshLength / Vector3.Distance(space.ToPlatform(offMeshStart.Value), space.ToPlatform(agent.currentOffMeshLinkData.endPos));

            var link = agent.currentOffMeshLinkData.offMeshLink.GetComponent<NavMeshLink>();
            if (link != null)
            {
                var point1 = link.NavPlatformPoint1;
                if (Vector3.Distance(point1, agent.currentOffMeshLinkData.startPos) < Vector3.Distance(point1, agent.currentOffMeshLinkData.endPos))
                {
                    offMeshStartPoint = point1;
                    offMeshEndPoint = link.NavPlatformPoint2;
                }
                else
                {
                    offMeshStartPoint = link.NavPlatformPoint2;
                    offMeshEndPoint = point1;
                }
            }
        }

        // Gets angle around y axis from a world space direction
        public float GetAngleFromForward(Transform transform, Vector3 worldDirection)
        {
            Vector3 local = transform.InverseTransformDirection(worldDirection);
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        public bool ExistsPath(Vector3 modelDestination)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (IsOnOffmeshLink)
            {
                return false;
            }

            if (!Agent.isOnNavMesh)
            {
                return false;
            }

            var to = space.ToProxy(modelDestination);

            NavMeshHit toHit;
            NavMesh.SamplePosition(to, out toHit, NavMeshSearchDistance, agent.areaMask);
            if (!toHit.hit)
            {
                return false;
            }

            var path = new NavMeshPath();
            return Agent.CalculatePath(toHit.position, path);
        }
    }
}