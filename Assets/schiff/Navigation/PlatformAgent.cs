
using RootMotion.FinalIK;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Assets.schiff
{
    public abstract class PlatformAgent : MonoBehaviour
    {
        public Animator Model;
        public float LocomotionBlendSpeed = 1;
    }
}