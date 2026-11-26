using System;
using UnityEngine;

namespace DivineSense
{
    public sealed class CrumblingPlatform : MonoBehaviour
    {
        public PlatformScroller Scroller;
        public SpriteRenderer Visual;

        private static readonly Color TimedColor = new Color(.9f, .57f, .32f);
        private float deadline = -1f;

        public event Action Started;

        private void OnCollisionEnter2D(Collision2D collision) => TryStartTimer(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryStartTimer(collision);

        private void TryStartTimer(Collision2D collision)
        {
            if (deadline >= 0f) return;
            var session = Scroller.Session;
            var player = collision.gameObject.GetComponent<PlayerContext>();
            if (!player || !player.CanAct || player.Session != session || collision.collider != player.Motor.Collider)
                return;

            // One-way colliders can report contacts while a player is passing upward through them.
            var body = player.Motor.Body;
            if (body.position.y < transform.position.y || body.linearVelocity.y > session.World.ScrollVelocity + .1f)
                return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                if (!contact.enabled || contact.normal.y >= -.5f) continue;
                deadline = session.Now + session.Settings.CrumbleDelay;
                Started?.Invoke();
                return;
            }
        }

        private void Update()
        {
            if (deadline < 0f || !Scroller.Session.IsPlaying) return;
            float now = Scroller.Session.Now;
            Visual.color = Color.Lerp(TimedColor, Color.red, Mathf.PingPong(now * 5f, 1f));
            if (now >= deadline) Destroy(gameObject);
        }
    }
}
