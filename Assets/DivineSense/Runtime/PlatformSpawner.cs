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
        [SerializeField] private Transform startingDais;
        [SerializeField, Min(0f), Tooltip("Use zero to keep the shared platform spacing.")]
        private float rowSpacingOverride;
        [SerializeField] private PlatformScroller hiddenPlatformPrefab;
        [SerializeField] private PlatformScroller soundBreakablePlatformPrefab;
        [SerializeField] private FollowerCollectible followerPrefab;

        private const float PlatformThickness = .35f;
        private const float HorizontalClearance = .25f;
        private const float VerticalClearance = 1.1f;
        private const float ObstacleChance = .35f;
        private const float SoundPlatformChance = .3f;
        private const int MaxRowsWithoutObstacle = 3;
        private const float OuterWidthScale = .58f;
        private const float OuterInwardShift = .16f;
        private const float OuterHorizontalJitter = 1.7f;
        private const float MissingOuterLaneChance = .4f;
        private const float MiddleBranchWidth = 2.2f;
        private int rowsSinceHidden = MaxRowsWithoutObstacle;
        private int rowsSinceSoundPlatform = MaxRowsWithoutObstacle;
        private float nextRowSpacing;
        private bool initialized;
        private float[] laneOffsets;
        private readonly List<Row> rows = new List<Row>();
        private bool UsesArenaLayout => Session.Trial == TrialLevel.Arena;

        // A row follows its surviving platforms rather than an elapsed-time estimate.
        private sealed class Row
        {
            public readonly List<Transform> Platforms = new List<Transform>();
            public readonly List<float> Offsets = new List<float>();
            private Transform anchor;
            private float anchorOffset;

            public void Add(Transform platform, float height)
            {
                Platforms.Add(platform);
                Offsets.Add(platform.position.y - height);
            }

            public void AnchorTo(Transform platform, float height)
            {
                anchor = platform;
                anchorOffset = platform.position.y - height;
            }

            public bool TryGetHeight(bool requireAnchor, out float height)
            {
                if (anchor)
                {
                    height = anchor.position.y - anchorOffset;
                    return true;
                }
                if (!requireAnchor)
                {
                    for (int i = 0; i < Platforms.Count; i++)
                        if (Platforms[i]) { height = Platforms[i].position.y - Offsets[i]; return true; }
                }
                height = 0;
                return false;
            }

            public void CullSurvivingPlatforms()
            {
                foreach (Transform platform in Platforms)
                {
                    if (!platform) continue;
                    platform.gameObject.SetActive(false);
                    Object.Destroy(platform.gameObject);
                }
            }
        }

        private void Start() => PopulateInitialPlatforms();

        public void PopulateInitialPlatforms()
        {
            if (!GeneratePlatforms || initialized) return;
            initialized = true;
            Rect bounds = Session.World.VisibleBounds;
            if (startingDais)
            {
                float daisHeight = startingDais.position.y;
                SpawnRow(daisHeight, isDaisRow: true);
                for (float y = daisHeight + SampleSpacing(); y < bounds.yMax + 1; y += SampleSpacing())
                    SpawnRow(y);
                for (float y = daisHeight - SampleSpacing(); y > bounds.yMin - 1; y -= SampleSpacing())
                    SpawnRow(y);
            }
            else
            {
                for (float y = bounds.yMin + 1; y < bounds.yMax + 1; y += SampleSpacing())
                    SpawnRow(y);
            }
            nextRowSpacing = SampleSpacing();
        }

        private float SampleSpacing()
        {
            GameSettings settings = Session.Settings;
            float spacing = rowSpacingOverride > 0 ? rowSpacingOverride : settings.PlatformSpacing;
            return Mathf.Max(.5f, spacing +
                Random.Range(-settings.PlatformSpacingVariation, settings.PlatformSpacingVariation));
        }

        private void Update()
        {
            if (!GeneratePlatforms || !Session.IsPlaying) return;
            float velocity = Session.World.ScrollVelocity;
            if (velocity == 0f) return;

            bool movingDown = velocity < 0f;
            bool trackStableRoute = UsesArenaLayout;
            float frontier = movingDown ? float.NegativeInfinity : float.PositiveInfinity;
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                if (!rows[i].TryGetHeight(trackStableRoute, out float height))
                {
                    // Once the central landing scrolls out, side remnants must not
                    // reserve this row or obstruct its replacement after a reversal.
                    if (trackStableRoute) rows[i].CullSurvivingPlatforms();
                    rows.RemoveAt(i);
                    continue;
                }
                frontier = movingDown ? Mathf.Max(frontier, height) : Mathf.Min(frontier, height);
            }

            Rect bounds = Session.World.VisibleBounds;
            float entry = movingDown ? bounds.yMax + 1 : bounds.yMin - 1;
            float gap = movingDown ? entry - frontier : frontier - entry;
            // On reversal, keep the existing rows and wait for real space at the new edge.
            if (gap < nextRowSpacing) return;
            if (SpawnRow(entry)) nextRowSpacing = SampleSpacing();
        }

        private bool SpawnRow(float y, bool isDaisRow = false)
        {
            GameSettings settings = Session.Settings;
            Rect bounds = Session.World.VisibleBounds;
            int lanes = Mathf.Max(1, settings.PlatformLaneCount);
            float usableWidth = Mathf.Max(1, bounds.width - settings.PlatformEdgeInset * 2);
            float laneWidth = usableWidth / lanes;
            float[] centers = new float[lanes];
            float[] widths = new float[lanes];
            var row = new Row();
            if (isDaisRow)
            {
                row.Add(startingDais, y);
                row.AnchorTo(startingDais, y);
            }

            int centerLane = lanes / 2;
            bool fragileSides = UsesArenaLayout;
            int skippedLane = -1;
            if (fragileSides && !isDaisRow && lanes > 2 && Random.value < MissingOuterLaneChance)
            {
                skippedLane = Random.Range(0, lanes - 1);
                if (skippedLane >= centerLane) skippedLane++;
            }

            if (laneOffsets == null || laneOffsets.Length != lanes)
            {
                laneOffsets = new float[lanes];
                for (int lane = 0; lane < lanes; lane++)
                    laneOffsets[lane] = Random.Range(-settings.PlatformLaneStagger,
                        settings.PlatformLaneStagger);
                if (startingDais) laneOffsets[centerLane] = 0f;
            }

            PlatformScroller SpawnLane(int lane)
            {
                bool isCenterLane = lane == centerLane;
                float width = Random.Range(settings.PlatformMinimumWidth, settings.PlatformWidth);
                if (fragileSides && !isCenterLane) width *= OuterWidthScale;
                width = Mathf.Min(width, laneWidth * .85f);
                float center = bounds.center.x - usableWidth * .5f + laneWidth * (lane + .5f);
                float horizontalJitter = settings.PlatformHorizontalJitter;
                if (fragileSides && !isCenterLane)
                {
                    center = Mathf.Lerp(center, bounds.center.x, OuterInwardShift);
                    horizontalJitter = Mathf.Max(horizontalJitter, OuterHorizontalJitter);
                }
                float jitter = Mathf.Min(horizontalJitter, (laneWidth - width) * .5f);
                centers[lane] = center + Random.Range(-jitter, jitter);
                float height = y + laneOffsets[lane] + Random.Range(-settings.PlatformVerticalJitter,
                    settings.PlatformVerticalJitter);
                var platform = TrySpawnPlatform(centers[lane], height, width, y, row,
                    stable: isCenterLane && fragileSides);
                if (!fragileSides || platform) widths[lane] = width;
                return platform;
            }

            if (fragileSides && !isDaisRow)
            {
                var centerPlatform = SpawnLane(centerLane);
                if (!centerPlatform) return false;
                row.AnchorTo(centerPlatform.transform, y);
            }

            for (int lane = 0; lane < lanes; lane++)
            {
                if (lane == skippedLane || (fragileSides && lane == centerLane)) continue;
                SpawnLane(lane);
            }

            // Sound can break this optional landing alongside the steady route.
            if (fragileSides && !isDaisRow && lanes > 1 && soundBreakablePlatformPrefab)
            {
                bool soundSpawned = (rowsSinceSoundPlatform >= MaxRowsWithoutObstacle || Random.value < SoundPlatformChance) &&
                    TrySpawnConnector(centers, widths, y, row, soundBreakablePlatformPrefab);
                rowsSinceSoundPlatform = soundSpawned ? 0 : rowsSinceSoundPlatform + 1;
            }

            bool hiddenSpawned = false;
            if (!isDaisRow && row.Platforms.Count > 0 && lanes > 1 && hiddenPlatformPrefab && followerPrefab)
            {
                if (rowsSinceHidden >= MaxRowsWithoutObstacle || Random.value < ObstacleChance)
                    hiddenSpawned = TrySpawnConnector(centers, widths, y, row, hiddenPlatformPrefab);
                rowsSinceHidden = hiddenSpawned ? 0 : rowsSinceHidden + 1;
            }

            if (!hiddenSpawned && lanes > 1 && Random.value < settings.PlatformConnectorChance)
                TrySpawnConnector(centers, widths, y, row);
            if (row.Platforms.Count == 0) return false;

            PopulateFollowers(row);
            rows.Add(row);
            return true;
        }

        private bool TrySpawnConnector(float[] centers, float[] widths, float y, Row row,
            PlatformScroller alternatePrefab = null)
        {
            GameSettings settings = Session.Settings;
            int gapCount = centers.Length - 1;
            int firstGap = Random.Range(0, gapCount);
            int centerLane = centers.Length / 2;
            bool hidden = alternatePrefab && alternatePrefab == hiddenPlatformPrefab;
            bool besideMiddle = alternatePrefab && !hidden;
            float height = y + settings.PlatformConnectorHeight;
            int gapsToTry = alternatePrefab || UsesArenaLayout ? gapCount : 1;
            for (int i = 0; i < gapsToTry; i++)
            {
                int gap = (firstGap + i) % gapCount;
                if (widths[gap] <= 0f || widths[gap + 1] <= 0f) continue;
                bool leftOfMiddle = gap + 1 == centerLane;
                if (besideMiddle && !leftOfMiddle && gap != centerLane) continue;

                float clearance = besideMiddle ? HorizontalClearance + .02f : HorizontalClearance;
                float left = centers[gap] + widths[gap] * .5f + clearance;
                float right = centers[gap + 1] - widths[gap + 1] * .5f - clearance;
                float width = Mathf.Min(besideMiddle ? MiddleBranchWidth : settings.PlatformMinimumWidth, right - left);
                if (width < 1f) continue;

                float x = (left + right) * .5f;
                if (besideMiddle)
                    x = leftOfMiddle ? right - width * .5f : left + width * .5f;
                if (!alternatePrefab)
                {
                    if (TrySpawnPlatform(x, height, width, y, row)) return true;
                    continue;
                }

                var position = new Vector2(x, height);
                var size = new Vector2(width, PlatformThickness);
                if (!HasClearance(position, size)) continue;

                var platform = SpawnScrolling(alternatePrefab, position, size);
                if (hidden)
                {
                    var follower = Instantiate(followerPrefab, platform.transform, false);
                    // Keep the collectible the same world size as the ones on the normal lanes.
                    follower.transform.localScale = new Vector3(.43f / width, 1.5f, 1f);
                    follower.Session = Session;
                    platform.GetComponent<MicroSense.HiddenPlatform>().SetRevealContent(follower.gameObject);
                }
                if (UsesArenaLayout) row.Add(platform.transform, y);
                return true;
            }
            return false;
        }

        private PlatformScroller TrySpawnPlatform(float x, float y, float width, float rowHeight, Row row,
            bool stable = false)
        {
            var position = new Vector2(x, y);
            var size = new Vector2(width, PlatformThickness);
            if (!HasClearance(position, size)) return null;

            GameSettings settings = Session.Settings;
            bool crumble = !stable && (UsesArenaLayout || Random.value < settings.CrumbleChance);
            var prefab = (crumble ? settings.CrumblePrefab : settings.PlatformPrefab).GetComponent<PlatformScroller>();
            var platform = SpawnScrolling(prefab, position, size);
            row.Add(platform.transform, rowHeight);
            return platform;
        }

        private PlatformScroller SpawnScrolling(PlatformScroller prefab, Vector2 position, Vector2 size)
        {
            var instance = Instantiate(prefab, position, Quaternion.identity, Root);
            instance.transform.localScale = new Vector3(size.x, size.y, 1);
            instance.Session = Session;
            return instance;
        }

        private bool HasClearance(Vector2 position, Vector2 size)
        {
            float halfWidth = size.x * .5f + HorizontalClearance;
            float halfHeight = size.y * .5f + VerticalClearance;
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
                if (position.x - halfWidth < occupied.max.x && position.x + halfWidth > occupied.min.x
                    && position.y - halfHeight < occupied.max.y && position.y + halfHeight > occupied.min.y)
                    return false;
            }
            return true;
        }

        private void PopulateFollowers(Row row)
        {
            foreach (Transform platform in row.Platforms)
            {
                if (platform == startingDais) continue;
                foreach (FollowerCollectible follower in platform.GetComponentsInChildren<FollowerCollectible>())
                {
                    follower.Session = Session;
                    if (Random.value > Session.Settings.FollowerDensity)
                        Destroy(follower.gameObject);
                }
            }
        }
    }
}
