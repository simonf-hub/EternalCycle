using UnityEngine;
using UnityEngine.SceneManagement;

namespace DivineSense
{
    public sealed class PlayerVisuals : MonoBehaviour
    {
        public PlayerContext Owner;
        public SpriteRenderer[] Renderers;

        [Header("Animation Sprites")]
        [SerializeField] private Sprite[] walkSprites;
        [SerializeField] private Sprite jumpSprite;
        [SerializeField] private Sprite[] abilitySprites;

        [Header("Cape")]
        [SerializeField] private SpriteRenderer cape;
        [SerializeField] private Sprite[] airborneCapeSprites;

        private const float WalkFramesPerSecond = 10f;
        private const float AbilityFramesPerSecond = 8f;
        private const float CapeFramesPerSecond = 8f;
        private const float MovementThreshold = .05f;
        private const int DashAfterimageCount = 8;
        private const float DashAfterimageSpacing = .28f;
        private const float DashAfterimageLifetime = .24f;
        private const float DashAfterimageOpacity = .47f;

        private struct DashAfterimage
        {
            public SpriteRenderer Renderer;
            public float StartedAt;
            public float Opacity;
        }

        private SpriteRenderer _body;
        private Sprite _idleSprite;
        private Sprite _baseCapeSprite;
        private float _walkTime;
        private float _capeFloatTime;
        private float _castStartedAt = -1f;
        private SpriteRenderer[] _readyGlow;
        private Color[] _baseColors;
        private LineRenderer _chargeTrack, _chargeFill;
        private Material _chargeMaterial;
        private TextMesh _stareCountdown, _stareCountdownShadow;
        private int _stareSeconds;
        private readonly DashAfterimage[] _dashAfterimages = new DashAfterimage[DashAfterimageCount];
        private Transform _dashAfterimagesRoot;
        private int _nextDashAfterimage;
        private float _lastDashStepAt = float.NegativeInfinity;
        private Vector3 _lastDashAfterimagePosition;

        private void OnEnable()
        {
            Owner.Abilities.Activated += StartCastAnimation;
            Owner.Motor.DashStepped += CaptureDashAfterimage;
            Owner.Motor.Teleported += ClearDashAfterimages;
        }

        private void Start()
        {
            _chargeMaterial = new Material(Shader.Find("Sprites/Default"));
            _chargeTrack = CreateChargeLine("Glide Charge Track", .13f, 40);
            _chargeFill = CreateChargeLine("Glide Charge Fill", .085f, 41);
            _stareCountdown = CreateStareText("Stare Countdown", transform, new Color(.4f, .9f, 1f), 51);
            _stareCountdownShadow = CreateStareText("Shadow", _stareCountdown.transform, Color.black, 50);
            _stareCountdownShadow.transform.localPosition = new Vector3(.025f, -.025f, 0f);
            _stareCountdown.gameObject.SetActive(false);
            _baseColors = new Color[Renderers.Length];
            for (int i = 0; i < Renderers.Length; i++)
                _baseColors[i] = Renderers[i].color;

            // Follow the body visual so growth, glide, and facing also affect the aura.
            if (Renderers.Length == 0) return;
            _body = Renderers[0];
            _idleSprite = _body.sprite;
            if (cape != null) _baseCapeSprite = cape.sprite;
            const int layers = 6;
            _readyGlow = new SpriteRenderer[layers];
            for (int i = 0; i < layers; i++)
            {
                var layer = new GameObject("Ability Ready Glow " + i);
                layer.transform.SetParent(_body.transform, false);
                layer.transform.localScale = Vector3.one * (1.12f + i * .09f);
                var glow = layer.AddComponent<SpriteRenderer>();
                glow.sharedMaterial = _body.sharedMaterial;
                glow.sortingLayerID = _body.sortingLayerID;
                glow.sortingOrder = _body.sortingOrder - layers + i - 1;
                glow.enabled = false;
                _readyGlow[i] = glow;
            }
        }

        private void LateUpdate()
        {
            UpdateBodySprite();
            UpdateCapeSprite();
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

            if (visible && Owner.CanAct) FadeDashAfterimages();
            else ClearDashAfterimages();
            UpdateChargeBar(visible);
            UpdateStareCountdown(visible);
            if (_readyGlow == null) return;
            Color tint = Color.Lerp(_baseColors[0], Color.white, .35f);
            Vector3 center = _body.sprite.bounds.center;
            if (_body.flipX) center.x = -center.x;
            for (int i = 0; i < _readyGlow.Length; i++)
            {
                SpriteRenderer glow = _readyGlow[i];
                glow.enabled = ready;
                glow.sprite = _body.sprite;
                glow.flipX = _body.flipX;
                glow.transform.localPosition = center * (1f - glow.transform.localScale.x);
                tint.a = (.065f + pulse * .025f) * (1f - i / (float)_readyGlow.Length);
                glow.color = tint;
            }
        }

        private void UpdateBodySprite()
        {
            if (_body == null) return;
            if (!Owner.CanAct)
            {
                _castStartedAt = -1f;
                _walkTime = 0f;
                _body.sprite = _idleSprite;
                return;
            }

            float direction = Owner.Input.Horizontal;
            bool moving = Mathf.Abs(direction) > MovementThreshold;
            if (moving)
                _body.flipX = Owner.Status.Reversed ? direction > 0f : direction < 0f;

            bool grounded = Owner.Motor.Grounded;
            Sprite pose = grounded ? _idleSprite : jumpSprite;
            int castFrame = _castStartedAt < 0f ? -1
                : Mathf.FloorToInt((Time.time - _castStartedAt) * AbilityFramesPerSecond);
            if (castFrame >= 0 && abilitySprites != null && castFrame < abilitySprites.Length &&
                !Owner.Status.Frozen)
            {
                Sprite cast = abilitySprites[castFrame];
                if (cast != null) pose = cast;
            }
            else if (grounded && moving && walkSprites != null && walkSprites.Length > 0 &&
                Mathf.Abs(Owner.Motor.Body.linearVelocity.x) > MovementThreshold)
            {
                int frame = Mathf.FloorToInt(_walkTime * WalkFramesPerSecond) % walkSprites.Length;
                _body.sprite = walkSprites[frame] != null ? walkSprites[frame] : _idleSprite;
                _walkTime += Time.deltaTime;
                return;
            }

            _walkTime = 0f;
            _body.sprite = pose != null ? pose : _idleSprite;
        }

        private void UpdateCapeSprite()
        {
            if (cape == null || _body == null) return;

            cape.flipX = _body.flipX;
            if (Owner.CanAct && !Owner.Motor.Grounded && airborneCapeSprites != null && airborneCapeSprites.Length > 0)
            {
                int frame = Mathf.FloorToInt(_capeFloatTime * CapeFramesPerSecond) % airborneCapeSprites.Length;
                Sprite floating = airborneCapeSprites[frame];
                cape.sprite = floating != null ? floating : _baseCapeSprite;
                _capeFloatTime += Time.deltaTime;
                return;
            }

            cape.sprite = _baseCapeSprite;
            _capeFloatTime = 0f;
        }

        private void StartCastAnimation(ActiveKind _) => _castStartedAt = Time.time;

        private void CaptureDashAfterimage()
        {
            if (_body == null || _body.sprite == null || Owner.Status.Hidden) return;

            // Use the physics pose so each fixed step leaves a distinct snapshot even when
            // several steps run between rendered frames.
            Transform source = _body.transform;
            Vector3 position = source.position + (Vector3)(Owner.Motor.Body.position - (Vector2)transform.position);
            float now = Time.fixedTime;
            bool continuingDash = now - _lastDashStepAt < Time.fixedDeltaTime * 1.5f;
            _lastDashStepAt = now;
            if (continuingDash && (position - _lastDashAfterimagePosition).sqrMagnitude <
                DashAfterimageSpacing * DashAfterimageSpacing) return;

            if (_dashAfterimagesRoot == null)
            {
                var root = new GameObject($"Dash Afterimages ({name})");
                if (root.scene != gameObject.scene) SceneManager.MoveGameObjectToScene(root, gameObject.scene);
                _dashAfterimagesRoot = root.transform;
            }

            ref DashAfterimage image = ref _dashAfterimages[_nextDashAfterimage];
            if (image.Renderer == null)
            {
                var snapshot = new GameObject("Dash Afterimage");
                snapshot.transform.SetParent(_dashAfterimagesRoot, false);
                image.Renderer = snapshot.AddComponent<SpriteRenderer>();
            }

            SpriteRenderer renderer = image.Renderer;
            renderer.transform.SetPositionAndRotation(position, source.rotation);
            renderer.transform.localScale = source.lossyScale;
            renderer.sprite = _body.sprite;
            renderer.flipX = _body.flipX;
            renderer.flipY = _body.flipY;
            renderer.sharedMaterial = _body.sharedMaterial;
            renderer.sortingLayerID = _body.sortingLayerID;
            renderer.sortingOrder = _body.sortingOrder - 1;
            Color tint = _body.color;
            tint.a *= DashAfterimageOpacity;
            renderer.color = tint;
            renderer.enabled = true;
            image.StartedAt = now;
            image.Opacity = tint.a;

            _lastDashAfterimagePosition = position;
            _nextDashAfterimage = (_nextDashAfterimage + 1) % DashAfterimageCount;
        }

        private void FadeDashAfterimages()
        {
            if (_dashAfterimagesRoot == null) return;
            float now = Time.time;
            foreach (DashAfterimage image in _dashAfterimages)
            {
                SpriteRenderer renderer = image.Renderer;
                if (renderer == null || !renderer.enabled) continue;

                float remaining = Mathf.Clamp01(1f - (now - image.StartedAt) / DashAfterimageLifetime);
                Color tint = renderer.color;
                tint.a = image.Opacity * remaining;
                renderer.color = tint;
                renderer.enabled = remaining > 0f;
            }
        }

        private void ClearDashAfterimages()
        {
            _lastDashStepAt = float.NegativeInfinity;
            _nextDashAfterimage = 0;
            if (_dashAfterimagesRoot == null) return;
            foreach (DashAfterimage image in _dashAfterimages)
                if (image.Renderer != null) image.Renderer.enabled = false;
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

        private static TextMesh CreateStareText(string label, Transform parent, Color color, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 52;
            text.fontStyle = FontStyle.Bold;
            text.characterSize = .1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = text.font.material;
            renderer.sortingOrder = order;
            return text;
        }

        private void UpdateStareCountdown(bool visible)
        {
            int seconds = visible ? Mathf.CeilToInt(Owner.Status.FreezeRemaining) : 0;
            _stareCountdown.gameObject.SetActive(seconds > 0);
            if (seconds == 0) return;

            Bounds bounds = Owner.Motor.Collider.bounds;
            _stareCountdown.transform.position = new Vector3(bounds.center.x, bounds.max.y + .4f, bounds.center.z);
            if (_stareSeconds == seconds) return;

            _stareSeconds = seconds;
            _stareCountdown.text = _stareCountdownShadow.text = seconds.ToString();
        }

        private void OnDestroy()
        {
            if (_chargeMaterial != null) Destroy(_chargeMaterial);
            if (_dashAfterimagesRoot != null) Destroy(_dashAfterimagesRoot.gameObject);
        }

        private void OnDisable()
        {
            Owner.Abilities.Activated -= StartCastAnimation;
            Owner.Motor.DashStepped -= CaptureDashAfterimage;
            Owner.Motor.Teleported -= ClearDashAfterimages;
            ClearDashAfterimages();
            _castStartedAt = -1f;
            _walkTime = 0f;
            _capeFloatTime = 0f;
            if (_chargeTrack != null) _chargeTrack.enabled = false;
            if (_chargeFill != null) _chargeFill.enabled = false;
            if (_stareCountdown != null) _stareCountdown.gameObject.SetActive(false);
            if (_readyGlow != null)
                foreach (var glow in _readyGlow)
                    if (glow != null) glow.enabled = false;
            if (_baseColors != null)
                for (int i = 0; i < Renderers.Length; i++)
                    if (Renderers[i] != null) Renderers[i].color = _baseColors[i];
        }
    }
}
