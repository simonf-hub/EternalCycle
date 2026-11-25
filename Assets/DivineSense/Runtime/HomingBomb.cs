using UnityEngine;

namespace DivineSense
{
    // A world-space projectile: only its designated opponent can detonate it.
    public sealed class HomingBomb : MonoBehaviour
    {
        private const float Radius = .22f;
        private PlayerContext target;
        private GameSession session;
        private float expiresAt;
        private Vector2 direction;
        private Material trailMaterial;

        public static void Launch(PlayerContext collector, Transform parent)
        {
            if (!collector || !collector.CanAct) return;
            PlayerContext opponent = collector.Registry.Opponent(collector);
            if (!opponent || !opponent.CanAct) return;
            var prefab = collector.Settings.HomingBombPrefab;
            var bomb = prefab
                ? Instantiate(prefab, collector.Endpoint, Quaternion.identity, parent)
                : CreateDefault(collector, parent);
            bomb.session = collector.Session;
            bomb.target = opponent;
            bomb.expiresAt = bomb.session.Now + Mathf.Max(.1f, collector.Settings.HomingBombLifetime);
            bomb.direction = (opponent.Endpoint - collector.Endpoint).normalized;
            if (bomb.direction.sqrMagnitude < .01f)
                bomb.direction = collector.Id == 0 ? Vector2.right : Vector2.left;
            bomb.AddDefaultTrail();
        }

        private static HomingBomb CreateDefault(PlayerContext collector, Transform parent)
        {
            var bombObject = new GameObject("Ares Homing Bomb");
            bombObject.transform.SetParent(parent, false);
            bombObject.transform.position = collector.Endpoint;
            var sprite = bombObject.AddComponent<SpriteRenderer>();
            sprite.sprite = collector.Settings.CircleSprite;
            sprite.color = GodPickup.PickupColor(GodKind.Ares);
            sprite.sortingOrder = 25;
            // Normalize sprite dimensions so artwork import scale does not change the hit radius.
            if (sprite.sprite)
            {
                Vector2 size = sprite.sprite.bounds.size;
                bombObject.transform.localScale = new Vector3(Radius * 2 / Mathf.Max(.01f, size.x),
                    Radius * 2 / Mathf.Max(.01f, size.y), 1);
            }
            return bombObject.AddComponent<HomingBomb>();
        }

        private void AddDefaultTrail()
        {
            if (GetComponentInChildren<TrailRenderer>(true)) return;

            trailMaterial = new Material(Shader.Find("Sprites/Default"));
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = .3f;
            trail.startWidth = Radius;
            trail.endWidth = 0;
            Color trailColor = GodPickup.PickupColor(GodKind.Ares);
            trail.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, .8f);
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0);
            trail.sortingOrder = 24;
        }

        private void FixedUpdate()
        {
            if (!session || !session.IsPlaying || !target || !target.CanAct || session.Now >= expiresAt)
            {
                Destroy(gameObject);
                return;
            }
            Vector2 position = transform.position;
            Vector2 offset = target.Endpoint - position;
            if (offset.sqrMagnitude > .0001f) direction = offset.normalized;
            float distance = Mathf.Min(offset.magnitude,
                Mathf.Max(.1f, session.Settings.HomingBombSpeed) * Time.fixedDeltaTime);
            bool hit = Vector2.Distance(position, target.Motor.Collider.ClosestPoint(position)) <= Radius;
            // Sweep the entire step to avoid tunneling at higher projectile speeds.
            if (!hit && distance > 0)
            {
                foreach (RaycastHit2D contact in Physics2D.CircleCastAll(position, Radius, direction, distance,
                    1 << target.gameObject.layer))
                    if (contact.collider == target.Motor.Collider) { hit = true; break; }
            }
            transform.position = position + direction * distance;
            if (!hit) return;
            target.Status.ApplyFreeze();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (trailMaterial) Destroy(trailMaterial);
        }
    }
}
