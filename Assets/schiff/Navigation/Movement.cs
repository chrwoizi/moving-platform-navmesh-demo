using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Assets.schiff
{
    [RequireComponent(typeof(PlatformNpc))]
    [RequireComponent(typeof(PlatformNav))]
    public class Movement : MonoBehaviour
    {
        void Update() {
            var npc = GetComponent<PlatformNpc>();
            var nav = GetComponent<PlatformNav>();
            var navProxy = nav.NavProxy;

            if(!navProxy.IsNavigating) {
                var r = new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10));
                var mainObjects = FindObjectOfType<MainObjects>();
                var dA = Vector3.Distance(transform.position, mainObjects.PlatformA.transform.position);
                var dB = Vector3.Distance(transform.position, mainObjects.PlatformB.transform.position);
                if(FindObjectOfType<UnityEngine.AI.OffMeshLink>() != null) {
                    if(dA > dB) {
                        npc.Navigate(mainObjects.PlatformA.transform.position + r, UnityEngine.Random.Range(0, 360));
                    }
                    else {
                        npc.Navigate(mainObjects.PlatformB.transform.position + r, UnityEngine.Random.Range(0, 360));
                    }
                }
                else {
                    if(dA < dB) {
                        npc.Navigate(mainObjects.PlatformA.transform.position + r, UnityEngine.Random.Range(0, 360));
                    }
                    else {
                        npc.Navigate(mainObjects.PlatformB.transform.position + r, UnityEngine.Random.Range(0, 360));
                    }
                }
            }
        }
    }
}