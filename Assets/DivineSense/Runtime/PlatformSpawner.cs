using System.Collections.Generic;
using UnityEngine;

namespace DivineSense
{
    public sealed class PlatformSpawner : MonoBehaviour
    {
        public GameSession Session;
        public Transform Root;
        [Tooltip("Disable to use only the platforms placed in this scene.")]
        public bool GeneratePlatforms = true;

        private const float PlatformThickness = .35f;
        private const float HorizontalClearance = .25f;
        private const float VerticalClearance = 1.1f;
        private float nextRowSpacing;
        private bool initialized;
        private float[] laneOffsets;
        private readonly List<Row> rows = new List<Row>();

        // A row follows its surviving platforms rather than an elapsed-time estimate.
        private sealed class Row
        {
            public readonly List<Transform> Platforms = new List<Transform>();
            public readonly List<float> Offsets = new List<float>();
            public bool TryGetHeight(out float height)
            {
                for (int i = 0; i < Platforms.Count; i++)
                    if (Platforms[i]) { height = Platforms[i].position.y - Offsets[i]; return true; }
                height = 0;
                return false;
            }
        }

        private void Start() => PopulateInitialPlatforms();

        public void PopulateInitialPlatforms()
        {
            if (!GeneratePlatforms || initialized) return;
            initialized = true;
            Rect bounds = Session.World.VisibleBounds;
            for (float y = bounds.yMin + 1; y < bounds.yMax + 1; y += SampleSpacing())
                SpawnRow(y);
            nextRowSpacing = SampleSpacing();
        }

        private float SampleSpacing()
        {
            GameSettings settings = Session.Settings;
            return Mathf.Max(.5f, settings.PlatformSpacing +
                Random.Range(-settings.PlatformSpacingVariation, settings.PlatformSpacingVariation));
        }

        private void Update()
        {
            if (!GeneratePlatforms || !Session.IsPlaying) return;
            bool heaven = Session.World.CurrentMode == SceneMode.Heaven;
            float frontier = heaven ? float.NegativeInfinity : float.PositiveInfinity;
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                if (!rows[i].TryGetHeight(out float height)) { rows.RemoveAt(i); continue; }
                frontier = heaven ? Mathf.Max(frontier, height) : Mathf.Min(frontier, height);
            }

            Rect bounds = Session.World.VisibleBounds;
            float entry = heaven ? bounds.yMax + 1 : bounds.yMin - 1;
            float gap = heaven ? entry - frontier : frontier - entry;
            // On reversal, keep the existing rows and wait for real space at the new edge.
            // At zero scroll speed the frontier never moves, so no extra rows accumulate.
            if (gap < nextRowSpacing) return;
            if (SpawnRow(entry)) nextRowSpacing = SampleSpacing();
        }

        private bool SpawnRow(float y)
        {
            GameSettings settings = Session.Settings;
            Rect bounds = Session.World.VisibleBounds;
            int lanes = Mathf.Max(1, settings.PlatformLaneCount);
            float usableWidth = Mathf.Max(1, bounds.width - settings.PlatformEdgeInset * 2);
            float laneWidth = usableWidth / lanes;
            float[] centers = new float[lanes];
            float[] widths = new float[lanes];
            var row = new Row();
            if (laneOffsets == null || laneOffsets.Length != lanes)
            {
                laneOffsets = new float[lanes];
                for (int lane = 0; lane < lanes; lane++)
                    laneOffsets[lane] = Random.Range(-settings.PlatformLaneStagger,
                        settings.PlatformLaneStagger);
            }

            // Propose a route in every lane, rejecting placements that crowd
            // existing support instead of forcing a full row into occupied space.
            for (int lane = 0; lane < lanes; lane++)
            {
                float width = Random.Range(settings.PlatformMinimumWidth, settings.PlatformWidth);
                width = Mathf.Min(width, laneWidth * .85f);
                float center = bounds.center.x - usableWidth * .5f + laneWidth * (lane + .5f);
                float jitter = Mathf.Min(settings.PlatformHorizontalJitter, (laneWidth - width) * .5f);
                centers[lane] = center + Random.Range(-jitter, jitter);
                widths[lane] = width;
                float height = y + laneOffsets[lane] + Random.Range(-settings.PlatformVerticalJitter,
                    settings.PlatformVerticalJitter);
                TrySpawn(centers[lane], height, width, y, row);
            }

            // Short, offset connectors let players switch lanes and race for followers.
            if (lanes > 1 && Random.value < settings.PlatformConnectorChance)
            {
                int gap = Random.Range(0, lanes - 1);
                float left = centers[gap] + widths[gap] * .5f + HorizontalClearance;
                float right = centers[gap + 1] - widths[gap + 1] * .5f - HorizontalClearance;
                float width = Mathf.Min(settings.PlatformMinimumWidth, right - left);
                // Fit connectors into the actual gap instead of overlapping adjacent lanes.
                if (width >= 1f)
                    TrySpawn((left + right) * .5f, y + settings.PlatformConnectorHeight, width, y, row);
            }
            if (row.Platforms.Count == 0) return false;
            rows.Add(row);
            return true;
        }

        private void TrySpawn(float x, float y, float width, float rowHeight, Row row)
        {
            // Include scene-authored support (the Starting Dais), earlier rows and connectors.
            // Collider bounds use the platforms' actual world sizes and current positions.
            var candidate = new Bounds(new Vector3(x, y, 0),
                new Vector3(width + HorizontalClearance * 2,
                    PlatformThickness + VerticalClearance * 2, 1));
            foreach (var existing in Root.GetComponentsInChildren<PlatformScroller>())
            {
                var collider = existing.GetComponent<Collider2D>();
                if (!collider || !collider.enabled) continue;
                // Read authored box geometry directly: newly spawned transforms may not
                // have been synchronized to the physics world until the next fixed step.
                Bounds occupied = collider is BoxCollider2D box
                    ? new Bounds(box.transform.TransformPoint(box.offset),
                        Vector3.Scale(box.size, box.transform.lossyScale))
                    : collider.bounds;
                if (candidate.min.x < occupied.max.x && candidate.max.x > occupied.min.x
                    && candidate.min.y < occupied.max.y && candidate.max.y > occupied.min.y)
                    return;
            }
            GameSettings settings = Session.Settings;
            bool crumble = Random.value < settings.CrumbleChance;
            GameObject platform = Instantiate(crumble ? settings.CrumblePrefab : settings.PlatformPrefab,
                new Vector3(x, y, 0), Quaternion.identity, Root);
            platform.transform.localScale = new Vector3(width, PlatformThickness, 1);
            platform.GetComponent<PlatformScroller>().Session = Session;
            row.Platforms.Add(platform.transform);
            row.Offsets.Add(y - rowHeight);
            foreach (FollowerCollectible follower in platform.GetComponentsInChildren<FollowerCollectible>())
            {
                follower.Session = Session;
                if (Random.value > settings.FollowerDensity) Destroy(follower.gameObject);
            }
        }
    }
}
