using UnityEngine;

namespace DivineSense
{
    public sealed class PlatformSpawner : MonoBehaviour
    {
        public GameSession Session;
        public Transform Root;

        private SceneMode lastMode;
        private float distanceSinceRow;
        private float nextRowSpacing;
        private bool initialized;
        private float[] laneOffsets;

        private void Start() => PopulateInitialPlatforms();

        public void PopulateInitialPlatforms()
        {
            if (initialized) return;
            initialized = true;
            Rect bounds = Session.World.VisibleBounds;
            for (float y = bounds.yMin + 1; y < bounds.yMax + 1; y += SampleSpacing())
                SpawnRow(y);
            ResetSchedule();
        }

        private void ResetSchedule()
        {
            lastMode = Session.World.CurrentMode;
            distanceSinceRow = 0;
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
            if (!Session.IsPlaying) return;
            if (lastMode != Session.World.CurrentMode) ResetSchedule();

            distanceSinceRow += Mathf.Abs(Session.World.ScrollVelocity) * Time.deltaTime;
            if (distanceSinceRow < nextRowSpacing) return;
            distanceSinceRow -= nextRowSpacing;
            nextRowSpacing = SampleSpacing();
            Rect bounds = Session.World.VisibleBounds;
            SpawnRow(lastMode == SceneMode.Heaven ? bounds.yMax + 1 : bounds.yMin - 1);
        }

        private void SpawnRow(float y)
        {
            GameSettings settings = Session.Settings;
            Rect bounds = Session.World.VisibleBounds;
            int lanes = Mathf.Max(1, settings.PlatformLaneCount);
            float usableWidth = Mathf.Max(1, bounds.width - settings.PlatformEdgeInset * 2);
            float laneWidth = usableWidth / lanes;
            float[] centers = new float[lanes];
            if (laneOffsets == null || laneOffsets.Length != lanes)
            {
                laneOffsets = new float[lanes];
                for (int lane = 0; lane < lanes; lane++)
                    laneOffsets[lane] = Random.Range(-settings.PlatformLaneStagger,
                        settings.PlatformLaneStagger);
            }

            // Every lane continues from row to row; jitter scatters the platforms
            // without making the basic route depend on Taste or Touch.
            for (int lane = 0; lane < lanes; lane++)
            {
                float width = Random.Range(settings.PlatformMinimumWidth, settings.PlatformWidth);
                width = Mathf.Min(width, laneWidth * .85f);
                float center = bounds.center.x - usableWidth * .5f + laneWidth * (lane + .5f);
                float jitter = Mathf.Min(settings.PlatformHorizontalJitter, (laneWidth - width) * .5f);
                centers[lane] = center + Random.Range(-jitter, jitter);
                Spawn(centers[lane], y + laneOffsets[lane] + Random.Range(-settings.PlatformVerticalJitter,
                    settings.PlatformVerticalJitter), width);
            }

            // Short, offset connectors let players switch lanes and race for followers.
            if (lanes > 1 && Random.value < settings.PlatformConnectorChance)
            {
                int gap = Random.Range(0, lanes - 1);
                float x = (centers[gap] + centers[gap + 1]) * .5f;
                Spawn(x, y + settings.PlatformConnectorHeight, settings.PlatformMinimumWidth);
            }
        }

        private void Spawn(float x, float y, float width)
        {
            GameSettings settings = Session.Settings;
            bool crumble = Random.value < settings.CrumbleChance;
            GameObject platform = Instantiate(crumble ? settings.CrumblePrefab : settings.PlatformPrefab,
                new Vector3(x, y, 0), Quaternion.identity, Root);
            platform.transform.localScale = new Vector3(width, .35f, 1);
            platform.GetComponent<PlatformScroller>().Session = Session;
            foreach (FollowerCollectible follower in platform.GetComponentsInChildren<FollowerCollectible>())
            {
                follower.Session = Session;
                if (Random.value > settings.FollowerDensity) Destroy(follower.gameObject);
            }
        }
    }
}
