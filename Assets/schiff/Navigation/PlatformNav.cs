using UnityEngine;
using System.Collections;
using System;
using UnityEngine.AI;

namespace Assets.schiff
{
    [RequireComponent(typeof(PlatformAgent))]
    public class PlatformNav : MonoBehaviour
    {
        public GameObject NavProxyPrefab;
        private PlatformAgent agent;
        private Animator model;

        [HideInInspector]
        public NavProxy NavProxy;

        private bool isInitializingNavigation;
        private bool isNavigating;

        public bool CanNavigate
        {
            get
            {
                return !NavProxy.IsOnOffmeshLink;
            }
        }

        void Awake()
        {
            agent = GetComponent<PlatformAgent>();
            model = agent.Model;
        }

        private void Start()
        {
            NavProxy = Instantiate(NavProxyPrefab).GetComponent<NavProxy>();
            NavProxy.Target = transform;
            NavProxy.TargetModel = model.transform;
            NavProxy.Animator = model;
        }

        private void OnDestroy()
        {
            if (NavProxy != null && NavProxy.gameObject != null)
            {
                Destroy(NavProxy.gameObject);
                NavProxy = null;
            }
        }

        public void StartNavigation(Vector3 destination, float? destinationRotation)
        {
            if (CanNavigate && !isInitializingNavigation)
            {
                StartCoroutine(Navigate(destination, destinationRotation));
            }
        }

        private IEnumerator Navigate(Vector3 destination, float? destinationRotation)
        {
            if (CanNavigate && !isInitializingNavigation)
            {
                isInitializingNavigation = true;

                if (!isNavigating)
                {
                    isNavigating = true;
                }

                yield return new WaitForSeconds(0.1f);

                if (CanNavigate)
                {
                    NavProxy.DestinationRotation = destinationRotation;

                    NavProxy.Navigate(destination, arrived =>
                    {
                        //NavProxy = null;
                        isNavigating = false;
                    });
                }

                isInitializingNavigation = false;
            }
        }

        public bool StopNavigation()
        {
            if (NavProxy.IsOnOffmeshLink)
            {
                return false;
            }

            if (isInitializingNavigation)
            {
                return false;
            }

            NavProxy.Stop();
            
            isNavigating = false;

            return true;
        }

        public bool ExistsPath(Vector3 destination)
        {
            return NavProxy.ExistsPath(destination);
        }
    }
}