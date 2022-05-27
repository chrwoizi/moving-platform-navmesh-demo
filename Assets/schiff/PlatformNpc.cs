


using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

namespace Assets.schiff
{
    [RequireComponent(typeof(PlatformNav))]
    public class PlatformNpc : PlatformAgent
    {
        private PlatformNav nav;

        public float RotateToTargetSpeed = 180;

        public float NavMeshSearchDistance = 2;

        public void Start()
        {
            nav = GetComponent<PlatformNav>();
        }

        public bool CanNavigateTo(Vector3 destination)
        {
            return nav.ExistsPath(destination);
        }

        public bool Navigate(Vector3 destination, float? destinationRotation)
        {
            if (nav.NavProxy == null) return false;
            if (!nav.NavProxy.IsOnOffmeshLink)
            {
                if (nav.NavProxy != null)
                {
                    if (Vector3.Distance(nav.NavProxy.GetCurrentModelDestination(), destination) < 0.1f)
                    {
                        return true;
                    }

                    if (!nav.StopNavigation())
                    {
                        return false;
                    }
                }

                nav.StartNavigation(destination, destinationRotation);
                return true;
            }
            Debug.Log(gameObject.name + " is on an offmesh link");

            return false;
        }
    }
}