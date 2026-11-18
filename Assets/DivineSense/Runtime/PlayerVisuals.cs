using UnityEngine;

namespace DivineSense
{
    public sealed class PlayerVisuals : MonoBehaviour
    {
        public PlayerContext Owner;
        public SpriteRenderer[] Renderers;

        private SpriteRenderer[] _readyGlow;
        private Color[] _baseColors;
        private LineRenderer _chargeTrack, _chargeFill;
        private Material _chargeMaterial;

        private void Start()
        {
            _chargeMaterial = new Material(Shader.Find("Sprites/Default"));
            _chargeTrack = CreateChargeLine("Glide Charge Track", .13f, 40);
            _chargeFill = CreateChargeLine("Glide Charge Fill", .085f, 41);
            _baseColors = new Color[Renderers.Length];
            for (int i = 0; i < Renderers.Length; i++)
                _baseColors[i] = Renderers[i].color;

            // Follow the body visual so growth, glide, and facing also affect the aura.
            if (Renderers.Length == 0) return;
            SpriteRenderer body = Renderers[0];
            const int layers = 6;
            _readyGlow = new SpriteRenderer[layers];
            for (int i = 0; i < layers; i++)
            {
                var layer = new GameObject("Ability Ready Glow " + i);
                layer.transform.SetParent(body.transform, false);
                layer.transform.localScale = Vector3.one * (1.12f + i * .09f);
                var glow = layer.AddComponent<SpriteRenderer>();
                glow.sprite = body.sprite;
                glow.sharedMaterial = body.sharedMaterial;
                glow.sortingLayerID = body.sortingLayerID;
                glow.sortingOrder = body.sortingOrder - layers + i - 1;
                glow.enabled = false;
                _readyGlow[i] = glow;
            }
        }

        private void LateUpdate()
        {
            bool visible = Owner.Life.State == LifeState.Alive && !Owner.Status.Hidden;
            bool ready = visible && Owner.CanAct && Owner.EquippedLoadout != null
                && Owner.Abilities.Cooldown <= 0f;
            float pulse = .5f + .5f * Mathf.Sin(Time.time * 3f);
            for (int i = 0; i < Renderers.Length; i++)
            {
                Renderers[i].enabled = visible;
                if (_baseColors != null)
                    Renderers[i].color = ready
                        ? Color.Lerp(_baseColors[i], Color.white, .12f + pulse * .08f)
                        : _baseColors[i];
            }

            UpdateChargeBar(visible);
            if (_readyGlow == null) return;
            Color tint = Color.Lerp(_baseColors[0], Color.white, .35f);
            for (int i = 0; i < _readyGlow.Length; i++)
            {
                _readyGlow[i].enabled = ready;
                tint.a = (.065f + pulse * .025f) * (1f - i / (float)_readyGlow.Length);
                _readyGlow[i].color = tint;
            }
        }

        private LineRenderer CreateChargeLine(string label, float width, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _chargeMaterial;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.sortingOrder = order;
            line.numCapVertices = 4;
            line.enabled = false;
            return line;
        }

        private void UpdateChargeBar(bool visible)
        {
            var glide = Owner.Motor.Glide;
            bool show = visible && Owner.CanAct && !Owner.Status.Frozen && glide.Charging
                && Owner.EquippedLoadout != null && Owner.EquippedLoadout.MovementKind == MovementKind.Touch;
            _chargeTrack.enabled = _chargeFill.enabled = show;
            if (!show) return;
            Bounds bounds = Owner.Motor.Collider.bounds;
            float width = Mathf.Max(.9f, bounds.size.x + .2f);
            var left = new Vector3(bounds.center.x - width * .5f, bounds.min.y - .22f, transform.position.z);
            _chargeTrack.SetPosition(0, left);
            _chargeTrack.SetPosition(1, left + Vector3.right * width);
            _chargeTrack.startColor = _chargeTrack.endColor = new Color(.08f, .1f, .18f, .95f);
            _chargeFill.SetPosition(0, left);
            _chargeFill.SetPosition(1, left + Vector3.right * width * Mathf.Max(.01f, glide.Charge01));
            Color color = Color.Lerp(new Color(.3f, .85f, 1f), new Color(1f, .85f, .3f), glide.Charge01);
            _chargeFill.startColor = _chargeFill.endColor = color;
        }

        private void OnDestroy() { if (_chargeMaterial != null) Destroy(_chargeMaterial); }

        private void OnDisable()
        {
            if (_chargeTrack != null) _chargeTrack.enabled = false;
            if (_chargeFill != null) _chargeFill.enabled = false;
            if (_readyGlow != null)
                foreach (var glow in _readyGlow)
                    if (glow != null) glow.enabled = false;
            if (_baseColors != null)
                for (int i = 0; i < Renderers.Length; i++)
                    if (Renderers[i] != null) Renderers[i].color = _baseColors[i];
        }
    }
}
