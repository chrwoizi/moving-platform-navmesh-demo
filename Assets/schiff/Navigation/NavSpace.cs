

using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Assets.schiff
{
    public class NavSpace : MonoBehaviour
    {
        [HideInInspector]
        public Transform platformA;
        [HideInInspector]
        public Transform platformB;

        [HideInInspector]
        public Transform platformANavigation;
        [HideInInspector]
        public Transform platformBNavigation;

        public bool IsMultiPlatform { get { 
            var main = FindObjectOfType<MainObjects>();
            return main.PlatformA != null && main.PlatformB != null; } 
        }

        void Start()
        {
            if (IsMultiPlatform)
            {
                var main = FindObjectOfType<MainObjects>();
                platformA = main.PlatformA.transform;
                platformB = main.PlatformB.transform;
                platformANavigation = main.PlatformA.NavMeshOrigin.transform;
                platformBNavigation = main.PlatformB.NavMeshOrigin.transform;
            }
        }

        public Vector3 ToPlatformA(Vector3 proxyWorld)
        {
            var local = platformANavigation.InverseTransformPoint(proxyWorld);
            var platformWorld = platformA.TransformPoint(local);
            return platformWorld;
        }

        public Vector3 ToPlatformB(Vector3 proxyWorld)
        {
            var local = platformBNavigation.InverseTransformPoint(proxyWorld);
            var platformWorld = platformB.TransformPoint(local);
            return platformWorld;
        }

        public Vector3 ToPlatform(Vector3 proxyWorld, Vector3 baseProxyWorld)
        {
            if (IsMultiPlatform)
            {
                if (baseProxyWorld.x < 0)
                {
                    return ToPlatformB(proxyWorld);
                }
                else
                {
                    return ToPlatformA(proxyWorld);
                }
            }
            else
            {
                return proxyWorld;
            }
        }

        public Vector3 ToPlatform(Vector3 proxyWorld)
        {
            return ToPlatform(proxyWorld, proxyWorld);
        }

        public Vector3 ToProxy(Vector3 platformWorld)
        {
            if (IsMultiPlatform)
            {
                Transform platform;
                Transform platformNavigation;
                
                var mainObjects = FindObjectOfType<MainObjects>();
                var dA = Mathf.Abs(platformWorld.x - mainObjects.PlatformA.transform.position.x);
                var dB = Mathf.Abs(platformWorld.x - mainObjects.PlatformB.transform.position.x);
                if (dA < dB)
                {
                    platform = platformA;
                    platformNavigation = platformANavigation;
                }
                else
                {
                    platform = platformB;
                    platformNavigation = platformBNavigation;
                }

                var local = platform.InverseTransformPoint(platformWorld);
                var proxyWorld = platformNavigation.TransformPoint(local);
                return proxyWorld;
            }
            else
            {
                return platformWorld;
            }
        }
    }
}