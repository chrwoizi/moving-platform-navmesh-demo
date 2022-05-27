using System.Collections;
using UnityEngine;

namespace Assets.schiff
{
    public class ActivatePhysicsLater : MonoBehaviour
    {
        public float Delay = 10;
        void Update()
        {
            GetComponent<Rigidbody>().isKinematic = Time.time < Delay;
        }
    }
}