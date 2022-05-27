using System.Collections;
using UnityEngine;

namespace Assets.schiff
{
    public class ShipWave : MonoBehaviour
    {
        public float PositionY;

        public float WaveSpeed = 0.1f;
        public float WaveMagnitude = 1f;
        public float WaveAngle = 0;
        private float lastWaveY = 0;

        public ParticleSystem[] Splashes;

        void Update()
        {
            if(FindObjectOfType<UnityEngine.AI.OffMeshLink>() != null) {
                return;
            }
            
            float waveY = 0;

            var waveAngle = WaveAngle + WaveSpeed * Time.deltaTime;
            while (waveAngle > 360) waveAngle -= 360;
            while (waveAngle < 0) waveAngle += 360;

            if (WaveAngle < 180 && waveAngle >= 180)
            {
                foreach (var item in Splashes)
                {
                    item.Emit(100);
                }
            }

            WaveAngle = waveAngle;

            waveY = Mathf.Sin(WaveAngle * Mathf.Deg2Rad) * WaveMagnitude;

            PositionY = waveY;
        }
    }

}