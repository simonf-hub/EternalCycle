using UnityEngine;

namespace DivineSense
{
    public sealed class FallingPickupMover : MonoBehaviour
    {
        public GodPickup Pickup;
        public Rigidbody2D Body;

        private void FixedUpdate()
        {
            if (Pickup.IsFalling && Pickup.Session.IsPlaying)
                Body.MovePosition(Body.position + Vector2.down * Pickup.Session.Settings.PickupFallSpeed * Time.fixedDeltaTime);
        }

        private void Update()
        {
            if (Pickup.IsFalling && transform.position.y < Pickup.Session.World.VisibleBounds.yMin - 1)
                Destroy(gameObject);
        }
    }
}
