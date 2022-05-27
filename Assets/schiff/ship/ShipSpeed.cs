using System.Collections;
using UnityEngine;

namespace Assets.schiff
{
    public class ShipSpeed : MonoBehaviour
    {
        public float PositionZ;

        public float RangeMin = -100;
        public float RangeMax = 100;
        public float RangeSweetSpot = 0;

        public float ForwardSpeed = 0;
        public float MaxSpeed = 0.1f;

        public float ForwardAcceleration = 0;
        public float MaxAcceleration = 0.1f;

        public float AccelerationChangeInterval = 1f;

        void Start()
        {
            StartCoroutine(ChangeSpeed());
        }

        private IEnumerator ChangeSpeed()
        {
            yield return new WaitForSeconds(AccelerationChangeInterval);

            float backwardProbability;
            if (PositionZ < RangeSweetSpot)
            {
                backwardProbability = 0.5f * (PositionZ - RangeMin) / (RangeSweetSpot - RangeMin);
            }
            else
            {
                backwardProbability = 0.5f + 0.5f * (PositionZ - RangeSweetSpot) / (RangeMax - RangeSweetSpot);
            }

            ForwardAcceleration = MaxAcceleration * (Random.value < backwardProbability ? -1 : 1) * Random.value;

            StartCoroutine(ChangeSpeed());
        }

        void Update()
        {
            if(FindObjectOfType<UnityEngine.AI.OffMeshLink>() != null) {
                return;
            }
            
            ForwardSpeed = ForwardSpeed + Time.deltaTime * ForwardAcceleration;
            if (ForwardSpeed > MaxSpeed)
            {
                ForwardSpeed = MaxSpeed;
            }

            PositionZ += Time.deltaTime * ForwardSpeed;
        }
    }

}