using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

namespace Assets.schiff
{
    [RequireComponent(typeof(ShipSpeed))]
    [RequireComponent(typeof(ShipWave))]
    public class Ship : MonoBehaviour
    {
        private ShipSpeed shipSpeed;
        private ShipWave shipWave;

        void Start()
        {
            shipSpeed = GetComponent<ShipSpeed>();
            shipWave = GetComponent<ShipWave>();
        }

        void Update()
        {
            var positionY = 0f;
            if (shipWave.enabled)
            {
                positionY = shipWave.PositionY;
            }

            var positionZ = 0f;
            if (shipSpeed.enabled)
            {
                positionZ = shipSpeed.PositionZ;
            }

            transform.position = new Vector3(transform.position.x, positionY, positionZ);
        }
    }
}