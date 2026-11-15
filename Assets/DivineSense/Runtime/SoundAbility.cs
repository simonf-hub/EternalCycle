using UnityEngine;

namespace DivineSense
{
    public sealed class SoundAbility : MonoBehaviour
    {
        private const int Segments = 96;
        private const float WaveDuration = .65f;
        private LineRenderer _wave;
        private LineRenderer _range;
        private Material _waveMaterial;
        private Vector3 _origin;
        private float _radius;
        private float _elapsed = WaveDuration;
        private readonly Color _waveColor = new Color(.45f, .9f, 1f);

        public void Activate(PlayerContext p)
        {
            // Show every cast, including casts that do not hit an opponent.
            ShowWave(p.Endpoint, p.Settings.SoundRadius);
            var t = p.Registry.Opponent(p);
            if (!t.CanAct || t.Status.Grown
                || Vector2.Distance(p.Endpoint, t.Endpoint) > p.Settings.SoundRadius) return;
            Vector2 d = t.Endpoint - p.Endpoint;
            if (d.sqrMagnitude < .01f) d = Vector2.right * (p.Id == 0 ? 1 : -1);
            t.Motor.QueueKnockback((d.normalized + Vector2.up * .3f) * p.Settings.SoundImpulse);
        }

        private void ShowWave(Vector2 origin, float radius)
        {
            if (_wave == null)
            {
                _waveMaterial = new Material(Shader.Find("Sprites/Default"));
                _range = CreateRing("Sound Range", .035f);
                _wave = CreateRing("Sound Wave", .075f);
            }
            // World-space rings stay at the cast position, matching the instant hit check.
            _origin = origin;
            _radius = Mathf.Max(0f, radius);
            _elapsed = 0f;
            DrawRing(_range, _radius, .3f);
            DrawRing(_wave, 0f, .9f);
        }

        private LineRenderer CreateRing(string label, float width)
        {
            var ring = new GameObject(label);
            ring.transform.SetParent(transform, false);
            var line = ring.AddComponent<LineRenderer>();
            line.sharedMaterial = _waveMaterial;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Segments;
            line.widthMultiplier = width;
            line.sortingOrder = 30;
            line.numCornerVertices = 2;
            line.enabled = false;
            return line;
        }

        private void Update()
        {
            if (_wave == null || _elapsed >= WaveDuration) return;
            _elapsed = Mathf.Min(WaveDuration, _elapsed + Time.deltaTime);
            float progress = _elapsed / WaveDuration;
            // The outer boundary shows the full gameplay range immediately.
            DrawRing(_range, _radius, .3f * (1f - progress));
            DrawRing(_wave, _radius * progress, .9f * (1f - progress));
            if (_elapsed >= WaveDuration) HideWave();
        }

        private void DrawRing(LineRenderer line, float radius, float alpha)
        {
            line.enabled = true;
            Color color = _waveColor;
            color.a = alpha;
            line.startColor = line.endColor = color;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                line.SetPosition(i, _origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }
        }

        private void HideWave()
        {
            _elapsed = WaveDuration;
            if (_wave != null) _wave.enabled = false;
            if (_range != null) _range.enabled = false;
        }

        private void OnDisable() => HideWave();

        private void OnDestroy()
        {
            if (_waveMaterial != null) Destroy(_waveMaterial);
        }
    }
}
