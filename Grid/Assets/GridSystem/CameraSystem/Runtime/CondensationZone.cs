using UnityEngine;

namespace GridTutorial.Cameras
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CondensationZone : MonoBehaviour
    {
        [SerializeField, Min(0)] float accumulationPerSecond = .08f;
        public float Rate => accumulationPerSecond;
        public bool Contains(Vector3 position)
        {
            var box = GetComponent<BoxCollider>();
            if (!isActiveAndEnabled || !box.enabled) return false;
            Vector3 p = transform.InverseTransformPoint(position) - box.center;
            Vector3 h = box.size * .5f;
            return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
        }
        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
    }
}
