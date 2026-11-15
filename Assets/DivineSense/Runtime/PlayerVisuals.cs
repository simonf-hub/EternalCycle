using UnityEngine;

namespace DivineSense
{
    public sealed class PlayerVisuals : MonoBehaviour
    {
        public PlayerContext Owner;
        public SpriteRenderer[] Renderers;

        private SpriteRenderer[] _readyGlow;
        private Color[] _baseColors;

        private void Start()
        {
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

            if (_readyGlow == null) return;
            Color tint = Color.Lerp(_baseColors[0], Color.white, .35f);
            for (int i = 0; i < _readyGlow.Length; i++)
            {
                _readyGlow[i].enabled = ready;
                tint.a = (.065f + pulse * .025f) * (1f - i / (float)_readyGlow.Length);
                _readyGlow[i].color = tint;
            }
        }

        private void OnDisable()
        {
            if (_readyGlow != null)
                foreach (var glow in _readyGlow)
                    if (glow != null) glow.enabled = false;
            if (_baseColors != null)
                for (int i = 0; i < Renderers.Length; i++)
                    if (Renderers[i] != null) Renderers[i].color = _baseColors[i];
        }
    }
}
