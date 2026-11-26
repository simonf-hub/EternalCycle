using UnityEngine;

namespace DivineSense
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ArenaBackdrop : MonoBehaviour
    {
        private const float EdgePadding = .5f;

        [SerializeField] private SceneModeController world;
        [SerializeField] private Sprite heaven;
        [SerializeField] private Sprite hell;

        private SpriteRenderer artwork;

        private void Start()
        {
            artwork = GetComponent<SpriteRenderer>();
            Vector3 position = transform.position;
            // Center the artwork across the full distance of the first scroll interval.
            position.y = world.VisibleBounds.center.y - world.ScrollVelocity * world.FlipInterval * .5f;
            transform.position = position;
        }

        private void FixedUpdate()
        {
            if (world.Session.IsPlaying)
                transform.position += Vector3.up * (world.ScrollVelocity * Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            Sprite sprite = world.CurrentMode == SceneMode.Heaven ? heaven : hell;
            if (artwork.sprite != sprite)
                artwork.sprite = sprite;

            Rect view = world.VisibleBounds;
            Vector3 position = transform.position;
            position.x = view.center.x;
            transform.position = position;

            // Cover the viewport throughout a full scroll interval, including narrow windows.
            Vector3 size = sprite.bounds.size;
            float phaseDistance = Mathf.Abs(world.ScrollVelocity) * world.FlipInterval;
            float distanceFromCenter = Mathf.Abs(position.y - view.center.y);
            float coverHeight = view.height + Mathf.Max(phaseDistance, 2f * distanceFromCenter);
            float scale = Mathf.Max((view.width + EdgePadding) / size.x, (coverHeight + EdgePadding) / size.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
