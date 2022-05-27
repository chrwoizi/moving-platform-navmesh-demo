using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Assets.schiff
{
    public class NavMeshLink : MonoBehaviour
    {
        public float Length = 4.8f;
        public float MaxNavMeshDistance = 1f;
        public GameObject OffMeshLinkTransformPrefab;
        public GameObject OffMeshLinkVisPrefab;

        private GameObject link1;
        private GameObject link2;
        private GameObject linkVis1;
        private GameObject linkVis2;

        private List<Collider> contactsWithPlatformA = new List<Collider>();
        private List<Collider> contactsWithPlatformB = new List<Collider>();

        private bool _hasOffMeshLink;
        bool hasOffMeshLink { get { return _hasOffMeshLink; } set { _hasOffMeshLink = value; OnHasOffMeshLinkChange(value); } }

        Vector3 offMeshLinkPos1;
        Vector3 offMeshLinkPos2;

        public Vector3 NavPlatformPoint1;
        public Vector3 NavPlatformPoint2;

        void OnHasOffMeshLinkChange(bool value)
        {
            if (value)
            {
                CreateOffMeshLink();
            }
            else
            {
                DestroyOffMeshLinks();
            }
        }

        void OnDestroy()
        {
            DestroyOffMeshLinks();
        }

        void OnCollisionEnter(Collision collision)
        {
            var hasLink = contactsWithPlatformA.Any() && contactsWithPlatformB.Any();

            var platform = collision.collider.GetComponentInParent<Platform>();
            if (platform != null)
            {
                var main = FindObjectOfType<MainObjects>();
                var platformA = main.PlatformA;
                var platformB = main.PlatformB;

                if (platform == platformA)
                {
                    contactsWithPlatformA.Add(collision.collider);
                }
                else if (platform == platformB)
                {
                    contactsWithPlatformB.Add(collision.collider);
                }

                if (!hasLink && contactsWithPlatformA.Any() && contactsWithPlatformB.Any())
                {
                    SetupOffMeshLink();
                }
            }
        }

        void OnCollisionExit(Collision collision)
        {
            var main = FindObjectOfType<MainObjects>();
            var platformA = main.PlatformA;
            var platformB = main.PlatformB;

            var platform = collision.collider.GetComponentInParent<Platform>();
            if (platform != null)
            {
                if (platform == platformA)
                {
                    contactsWithPlatformA.Remove(collision.collider);
                }
                else if (platform == platformB)
                {
                    contactsWithPlatformB.Remove(collision.collider);
                }
            }

            if (!contactsWithPlatformA.Any() || !contactsWithPlatformB.Any())
            {
                TeardownOffMeshLink();
            }
        }

        private bool SetupOffMeshLink()
        {
            TeardownOffMeshLink();

            var main = FindObjectOfType<MainObjects>();
            var platformA = main.PlatformA;
            var platformB = main.PlatformB;

            var navigationA = platformA.NavMeshOrigin.gameObject;
            var navigationB = platformB.NavMeshOrigin.gameObject;

            var point1 = transform.TransformPoint(new Vector3(-Length / 2, 0, 0));
            var point2 = transform.TransformPoint(new Vector3(Length / 2, 0, 0));

            Transform platform1;
            Transform platform2;
            Transform navigation1;
            Transform navigation2;
            int area1;
            int area2;

            if (point1.x > point2.x)
            {
                area1 = UnityEngine.AI.NavMesh.GetAreaFromName("PlatformA");
                area2 = UnityEngine.AI.NavMesh.GetAreaFromName("PlatformB");
                platform1 = platformA.transform;
                platform2 = platformB.transform;
                navigation1 = navigationA.transform;
                navigation2 = navigationB.transform;
            }
            else
            {
                area1 = UnityEngine.AI.NavMesh.GetAreaFromName("PlatformB");
                area2 = UnityEngine.AI.NavMesh.GetAreaFromName("PlatformA");
                platform1 = platformB.transform;
                platform2 = platformA.transform;
                navigation1 = navigationB.transform;
                navigation2 = navigationA.transform;
            }

            point1 = platform1.InverseTransformPoint(point1);
            point1 = navigation1.TransformPoint(point1);

            point2 = platform2.InverseTransformPoint(point2);
            point2 = navigation2.TransformPoint(point2);

            UnityEngine.AI.NavMeshHit hit1;
            if (!UnityEngine.AI.NavMesh.SamplePosition(point1, out hit1, MaxNavMeshDistance, 1 << area1))
            {
                Debug.Log("Failed to find navmesh position for point1");
                return false;
            }

            UnityEngine.AI.NavMeshHit hit2;
            if (!UnityEngine.AI.NavMesh.SamplePosition(point2, out hit2, MaxNavMeshDistance, 1 << area2))
            {
                Debug.Log("Failed to find navmesh position for point2");
                return false;
            }

            if (OffMeshLinkVisPrefab != null)
            {
                linkVis1 = (GameObject)Instantiate(OffMeshLinkVisPrefab, platform1.TransformPoint(navigation1.InverseTransformPoint(hit1.position)), Quaternion.identity);
                linkVis2 = (GameObject)Instantiate(OffMeshLinkVisPrefab, platform2.TransformPoint(navigation2.InverseTransformPoint(hit2.position)), Quaternion.identity);
            }

            offMeshLinkPos1 = hit1.position;
            offMeshLinkPos2 = hit2.position;
            NavPlatformPoint1 = point1;
            NavPlatformPoint2 = point2;
            hasOffMeshLink = true;

            return true;
        }

        private void TeardownOffMeshLink()
        {
            hasOffMeshLink = false;
        }

        private void CreateOffMeshLink()
        {
            link1 = (GameObject)Instantiate(OffMeshLinkTransformPrefab, offMeshLinkPos1, Quaternion.identity);
            link2 = (GameObject)Instantiate(OffMeshLinkTransformPrefab, offMeshLinkPos2, Quaternion.identity);

            var link = gameObject.AddComponent<UnityEngine.AI.OffMeshLink>();
            link.startTransform = link1.transform;
            link.endTransform = link2.transform;
            link.UpdatePositions();
            link.activated = true;
        }

        private void DestroyOffMeshLinks()
        {
            var links = GetComponents<UnityEngine.AI.OffMeshLink>();
            foreach (var link in links)
            {
                Destroy(link);
            }

            if (link1 != null)
            {
                Destroy(link1.gameObject);
            }

            if (link2 != null)
            {
                Destroy(link2.gameObject);
            }

            if (linkVis1 != null)
            {
                Destroy(linkVis1.gameObject);
            }

            if (linkVis2 != null)
            {
                Destroy(linkVis2.gameObject);
            }
        }
    }
}