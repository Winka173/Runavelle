using System.Collections.Generic;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>
    /// Pooled sprite particles, ground decals and floating labels. Element impacts follow ElementGrammar (shape +
    /// motion per element, not only colour). Quality level scales particle counts only; nothing here touches the
    /// simulation. Damage numbers are decided by Core's DamageTextAggregator and only drawn here.
    /// </summary>
    public sealed class Vfx
    {
        private sealed class P
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel;
            public float Life, Max, Size, EndSize, Spin, Gravity, Drag, Rot;
            public Color Col;
            public bool Active, Align;
        }

        private sealed class D
        {
            public SpriteRenderer Sr;
            public float Life, Max;
            public Color Col;
        }

        public sealed class FloatText
        {
            public Vector2 World;
            public string Text;
            public Color Col;
            public float Life, Max, Size;
            public int TargetId;
        }

        private readonly Transform _root;
        private readonly List<P> _ps = new List<P>();
        private readonly List<D> _decals = new List<D>();
        public readonly List<FloatText> Texts = new List<FloatText>();
        public readonly DamageTextAggregator Damage = new DamageTextAggregator();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private int _level = 2;
        private int _freeHint;
        public int ActiveCount { get; private set; }
        public const int MaxDecals = 48;

        public Vfx(Transform root)
        {
            _root = new GameObject("Vfx").transform;
            _root.SetParent(root, false);
        }

        private Sprite S(string key)
        {
            if (!_sprites.TryGetValue(key, out var s)) { s = ArtLibrary.Get(key); _sprites[key] = s; }
            return s;
        }

        private P Emit(string sprite, Vector2 pos, Vector2 vel, float life, float size, float endSize, Color col, int order = 30500, float gravity = 0f, bool align = false, float drag = 0f)
        {
            int cap = _level == 0 ? 120 : _level == 1 ? 300 : 600;
            if (ActiveCount >= cap) return null;
            P p = null;
            for (int k = 0; k < _ps.Count; k++)
            {
                int i = (_freeHint + k) % _ps.Count;
                if (!_ps[i].Active) { p = _ps[i]; _freeHint = i + 1; break; }
            }
            if (p == null)
            {
                var go = new GameObject("p");
                go.transform.SetParent(_root, false);
                p = new P { Sr = go.AddComponent<SpriteRenderer>() };
                _ps.Add(p);
            }
            p.Active = true;
            p.Sr.gameObject.SetActive(true);
            p.Sr.sprite = S(sprite);
            p.Sr.sortingOrder = order;
            p.Sr.flipX = false;
            p.Pos = pos; p.Vel = vel; p.Life = 0; p.Max = life; p.Size = size; p.EndSize = endSize; p.Col = col; p.Gravity = gravity; p.Align = align; p.Drag = drag;
            p.Spin = align ? 0f : Random.Range(-180f, 180f);
            p.Rot = Random.Range(0f, 360f);
            ActiveCount++;
            return p;
        }

        public void Update(float dt, GameSettings s)
        {
            _level = s.VfxLevel;
            Damage.Mode = s.DamageText;
            ActiveCount = 0;
            foreach (var p in _ps)
            {
                if (!p.Active) continue;
                p.Life += dt;
                if (p.Life >= p.Max) { p.Active = false; p.Sr.gameObject.SetActive(false); continue; }
                ActiveCount++;
                p.Vel.y -= p.Gravity * dt;
                if (p.Drag > 0f) p.Vel *= Mathf.Max(0f, 1f - p.Drag * dt);
                p.Pos += p.Vel * dt;
                float t = p.Life / p.Max;
                float sz = Mathf.Lerp(p.Size, p.EndSize, t);
                p.Sr.transform.position = new Vector3(p.Pos.x, p.Pos.y, 0);
                p.Sr.transform.localScale = new Vector3(sz, sz, 1);
                float rot = p.Align && p.Vel.sqrMagnitude > 0.0001f ? Mathf.Atan2(p.Vel.y, p.Vel.x) * Mathf.Rad2Deg : p.Rot + p.Spin * p.Life;
                p.Sr.transform.rotation = Quaternion.Euler(0, 0, rot);
                var c = p.Col;
                c.a *= 1f - t * t;
                p.Sr.color = c;
            }
            for (int i = _decals.Count - 1; i >= 0; i--)
            {
                var d = _decals[i];
                if (d.Max <= 0f) continue; // persistent until cleared
                d.Life += dt;
                var c = d.Col; c.a *= Mathf.Clamp01((d.Max - d.Life) / 1.5f);
                d.Sr.color = c;
                if (d.Life >= d.Max) { Object.Destroy(d.Sr.gameObject); _decals.RemoveAt(i); }
            }
            for (int i = Texts.Count - 1; i >= 0; i--)
            {
                var ft = Texts[i];
                ft.Life += dt;
                ft.World.y += dt * 0.6f;
                if (ft.Life >= ft.Max) Texts.RemoveAt(i);
            }
            Damage.Update(dt);
        }

        private int Count(int high) => _level == 0 ? Mathf.Max(1, high / 4) : _level == 1 ? Mathf.Max(1, high / 2) : high;

        public void Burst(Vector2 at, Color col, int n = 10, float speed = 2.5f, float size = 0.35f, string sprite = "fx/spark")
        {
            n = Count(n);
            for (int i = 0; i < n; i++)
            {
                var dir = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * speed;
                Emit(sprite, at + new Vector2(0, 0.4f), dir, Random.Range(0.25f, 0.55f), size, 0.05f, col);
            }
        }

        public void Ring(Vector2 at, Color col, float radius, float life = 0.45f, string sprite = "fx/ring")
        {
            Emit(sprite, at, Vector2.zero, life, radius * 0.4f, radius * 2.2f, col, 30400);
        }

        public void Smoke(Vector2 at, int n = 4, Color? col = null)
        {
            n = Count(n);
            for (int i = 0; i < n; i++)
                Emit("fx/smoke", at + Random.insideUnitCircle * 0.3f + new Vector2(0, 0.3f), new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 0.9f)), Random.Range(0.5f, 0.9f), 0.4f, 0.9f,
                    col ?? new Color(0.85f, 0.82f, 0.8f, 0.7f), 30300);
        }

        public void Leaf(Vector2 at)
        {
            Emit("fx/leaf", at, new Vector2(Random.Range(0.3f, 0.8f), -0.35f), 3f, 0.3f, 0.25f, new Color(0.55f, 0.75f, 0.35f, 0.9f), 31700, 0f, false, 0.2f);
        }

        public void Trail(Vector2 at, Color col)
        {
            Emit("fx/glow", at, Vector2.zero, 0.25f, 0.25f, 0.05f, new Color(col.r, col.g, col.b, 0.6f), 29900);
        }

        public void ZoneMote(Vector2 at, float r, Color col)
        {
            var p = at + Random.insideUnitCircle * r;
            Emit("fx/spark", p, new Vector2(0, Random.Range(0.3f, 0.8f)), 0.6f, 0.15f, 0.02f, new Color(col.r, col.g, col.b, 0.8f), 30200);
        }

        public void Slash(Vector2 at, float facing, Color col, float scale = 1f)
        {
            var p = Emit("fx/slash", at + new Vector2(facing * 0.45f, 0.45f), Vector2.zero, 0.18f, 0.6f * scale, 0.9f * scale, col, 30600);
            if (p != null) { p.Spin = 0; p.Rot = 0; p.Sr.flipX = facing < 0; }
        }

        /// <summary>Jagged arc between two points (chains, Electrocharge, Haunted Arc).</summary>
        public void Arc(Vector2 from, Vector2 to, Color col)
        {
            var d = to - from;
            float len = d.magnitude;
            if (len < 0.05f) return;
            int segs = Mathf.Clamp(Mathf.CeilToInt(len / 0.9f), 1, 6);
            for (int i = 0; i < segs; i++)
            {
                var a = from + d * (i / (float)segs) + Random.insideUnitCircle * 0.12f + new Vector2(0, 0.45f);
                Emit("fx/arcseg", a + d / segs * 0.5f, d.normalized * 0.01f, 0.16f, len / segs * 1.1f, len / segs * 1.1f, col, 30700, 0f, true);
            }
        }

        /// <summary>Element impact by grammar: Fire embers rise, Water splashes fall, Lightning zigzags, Ice shards burst,
        /// Wind spirals, Earth fragments fall, Light rays radiate, Shadow wisps sink.</summary>
        public void Element(Vector2 at, ElementId e, float scale = 1f)
        {
            if (e == ElementId.None) { Burst(at, new Color(1, 1, 0.9f), 4, 1.5f, 0.2f * scale); return; }
            var c = ArtLibrary.ElementColor(e);
            var g = ElementGrammar.For(e);
            var o = at + new Vector2(0, 0.4f);
            int n = Count(Mathf.RoundToInt(7 * Mathf.Clamp(scale, 0.5f, 2.5f)));
            switch (g.Motion)
            {
                case FxMotion.Rise:
                    for (int i = 0; i < n; i++) Emit("fx/ember", o + Random.insideUnitCircle * 0.25f * scale, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(1.2f, 2.4f)), Random.Range(0.4f, 0.75f), 0.32f * scale, 0.05f, c, 30500, -0.8f, true);
                    Emit("fx/glow", o, Vector2.zero, 0.25f, 0.6f * scale, 0.1f, new Color(c.r, c.g, c.b, 0.7f));
                    break;
                case FxMotion.Splash:
                    for (int i = 0; i < n; i++)
                    {
                        float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                        Emit("fx/drop", o, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.6f, 2.8f), Random.Range(0.35f, 0.6f), 0.22f * scale, 0.12f, c, 30500, 7f, true);
                    }
                    Ring(at, c, 0.45f * scale, 0.3f);
                    break;
                case FxMotion.Zigzag:
                    for (int i = 0; i < Mathf.Max(2, n / 2); i++)
                    {
                        var end = o + Random.insideUnitCircle.normalized * Random.Range(0.5f, 1.1f) * scale;
                        Arc(o - new Vector2(0, 0.45f), end - new Vector2(0, 0.45f), c);
                    }
                    break;
                case FxMotion.Burst:
                    for (int i = 0; i < n; i++)
                        Emit("fx/shard", o, Random.insideUnitCircle.normalized * Random.Range(1.8f, 3f), Random.Range(0.25f, 0.45f), 0.4f * scale, 0.15f, c, 30500, 0f, true, 3f);
                    break;
                case FxMotion.Spiral:
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)n * Mathf.PI * 2f;
                        var radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var tangential = new Vector2(-radial.y, radial.x);
                        Emit("fx/swirl", o + radial * 0.3f, (tangential * 2.2f + radial * 0.6f) * scale, 0.45f, 0.3f * scale, 0.55f * scale, c, 30500, 0f, false);
                    }
                    break;
                case FxMotion.Fall:
                    for (int i = 0; i < n; i++)
                        Emit("fx/chip", o, new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(1.5f, 3f)), Random.Range(0.4f, 0.65f), 0.3f * scale, 0.22f * scale, c, 30500, 9f);
                    Smoke(at, 2, new Color(0.65f, 0.55f, 0.4f, 0.5f));
                    break;
                case FxMotion.Radial:
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)n * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                        Emit("fx/ray", o, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.4f, 0.3f, 0.5f * scale, 0.9f * scale, c, 30600, 0f, true);
                    }
                    Ring(at + new Vector2(0, 0.3f), c, 0.4f * scale, 0.3f);
                    break;
                case FxMotion.Sink:
                    for (int i = 0; i < n; i++)
                        Emit("fx/wisp", o + Random.insideUnitCircle * 0.45f * scale, new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.6f, -0.2f)), Random.Range(0.5f, 0.85f), 0.45f * scale, 0.8f * scale, new Color(c.r * 0.7f, c.g * 0.6f, c.b * 0.9f, 0.8f), 30450);
                    break;
            }
        }

        /// <summary>Reaction showcase: a distinct silhouette per reaction, scaled by its weight.</summary>
        public void Reaction(Vector2 at, ReactionId r, ElementId e, Vector2? other = null)
        {
            var fx = ElementGrammar.For(r);
            float s = fx.Weight >= 2 ? 1.6f : 1.1f;
            var c = ArtLibrary.ElementColor(e == ElementId.None ? ElementId.Lightning : e);
            var o = at + new Vector2(0, 0.35f);
            switch (fx.Showcase)
            {
                case ReactionShowcase.Steam: Smoke(at, 8, new Color(0.95f, 0.97f, 1f, 0.75f)); Element(at, ElementId.Water, 0.7f); break;
                case ReactionShowcase.Blast:
                    Ring(at, new Color(1f, 0.6f, 0.2f), 1.4f * s, 0.4f, "fx/ring_thick");
                    Element(at, ElementId.Fire, s); Element(at, ElementId.Lightning, 0.8f); break;
                case ReactionShowcase.MeltPool:
                    Smoke(at, 6, new Color(0.95f, 0.95f, 1f, 0.7f)); Element(at, ElementId.Water, 1.1f); Decal("fx/puddle", at, 1.2f, 5f, new Color(1, 1, 1, 0.8f)); break;
                case ReactionShowcase.Vortex:
                    Element(at, ElementId.Wind, s); Element(at, ElementId.Fire, s); Ring(at, new Color(1f, 0.5f, 0.2f), 1.2f * s, 0.6f); break;
                case ReactionShowcase.Eruption:
                    Element(at, ElementId.Earth, s); Element(at, ElementId.Fire, 1.2f); Decal("fx/scorch", at, 1.3f, 8f, new Color(1, 1, 1, 0.75f)); break;
                case ReactionShowcase.ChainArcs:
                case ReactionShowcase.HauntArcs:
                    if (other.HasValue) Arc(at, other.Value, c);
                    Element(at, fx.Showcase == ReactionShowcase.HauntArcs ? ElementId.Shadow : ElementId.Lightning, 0.8f); break;
                case ReactionShowcase.ShardCage:
                    for (int i = 0; i < Count(10); i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f;
                        Emit("fx/shard", o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.9f, -new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.8f, 0.4f, 0.45f, 0.3f, ArtLibrary.ElementColor(ElementId.Ice), 30600, 0f, true, 3f);
                    }
                    Decal("fx/frost", at, 1.2f, 5f, new Color(1, 1, 1, 0.8f)); break;
                case ReactionShowcase.Downpour:
                    for (int i = 0; i < Count(12); i++)
                        Emit("fx/drop", o + new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(1.2f, 2f)), new Vector2(0, -5f), 0.35f, 0.18f, 0.18f, ArtLibrary.ElementColor(ElementId.Water), 30500, 0f, true);
                    Decal("fx/puddle", at, 1.4f, 5f, new Color(1, 1, 1, 0.7f)); break;
                case ReactionShowcase.Quagmire: Smoke(at, 4, new Color(0.45f, 0.35f, 0.2f, 0.7f)); Element(at, ElementId.Earth, 0.8f); break;
                case ReactionShowcase.Shatter: Element(at, ElementId.Ice, s); Element(at, ElementId.Lightning, 0.7f); break;
                case ReactionShowcase.StormCell:
                    for (int i = 0; i < 4; i++) Arc(at + Random.insideUnitCircle * 1.2f, at + Random.insideUnitCircle * 1.2f, ArtLibrary.ElementColor(ElementId.Lightning));
                    Ring(at, ArtLibrary.ElementColor(ElementId.Wind), 1.3f, 0.5f); break;
                case ReactionShowcase.Pull:
                    for (int i = 0; i < Count(8); i++)
                    {
                        var p0 = Random.insideUnitCircle.normalized * 1.3f;
                        Emit("fx/chip", o + p0, -p0 * 2.4f, 0.45f, 0.25f, 0.15f, ArtLibrary.ElementColor(ElementId.Earth), 30500);
                    }
                    break;
                case ReactionShowcase.Whiteout: Element(at, ElementId.Wind, s); Element(at, ElementId.Ice, s); Decal("fx/frost", at, 1.6f, 6f, new Color(1, 1, 1, 0.7f)); break;
                case ReactionShowcase.Spikes:
                    for (int i = 0; i < Count(6); i++)
                        Emit("fx/shard", at + new Vector2(Random.Range(-0.8f, 0.8f), 0), new Vector2(0, 3.2f), 0.3f, 0.5f, 0.3f, ArtLibrary.ElementColor(ElementId.Ice), 30600, 6f, true);
                    break;
                case ReactionShowcase.Dust: Smoke(at, 8, new Color(0.85f, 0.75f, 0.5f, 0.7f)); Element(at, ElementId.Wind, 1f); break;
                case ReactionShowcase.Eclipse:
                    Ring(at, new Color(0.3f, 0.15f, 0.45f), 1.5f, 0.6f, "fx/ring_thick"); Element(at, ElementId.Shadow, s); Element(at, ElementId.Light, 0.8f); break;
                case ReactionShowcase.Bulwark: Ring(at, ArtLibrary.ElementColor(ElementId.Light), 0.9f, 0.5f, "fx/ring_thick"); Element(at, ElementId.Earth, 0.7f); break;
                case ReactionShowcase.Flare: Element(at, ElementId.Light, s * 1.2f); Ring(at, new Color(1f, 0.95f, 0.6f), 1.6f, 0.4f); break;
                case ReactionShowcase.SoulChill: Element(at, ElementId.Shadow, 1f); Element(at, ElementId.Ice, 0.8f); break;
                case ReactionShowcase.Cleanse: Element(at, ElementId.Light, 1f); Element(at, ElementId.Water, 0.7f); break;
                default: Ring(at, c, 0.8f, 0.4f); Element(at, e, 0.8f); break;
            }
        }

        /// <summary>Ground decal (rubble/scorch/frost/puddle). life &lt;= 0 keeps it until ClearDecals (siege aftermath).</summary>
        public void Decal(string key, Vector2 at, float scale, float life, Color col)
        {
            if (_level == 0 && life > 0f) return;
            if (_decals.Count >= MaxDecals)
            {
                int idx = _decals.FindIndex(x => x.Max > 0f);
                if (idx < 0) idx = 0;
                Object.Destroy(_decals[idx].Sr.gameObject);
                _decals.RemoveAt(idx);
            }
            var go = new GameObject("decal");
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(key);
            sr.sortingOrder = WorldView.OrderDecal;
            sr.color = col;
            go.transform.position = new Vector3(at.x, at.y, 0);
            go.transform.localScale = new Vector3(scale, scale, 1);
            go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-12f, 12f));
            _decals.Add(new D { Sr = sr, Life = 0f, Max = life, Col = col });
        }

        public void ClearDecals()
        {
            foreach (var d in _decals) Object.Destroy(d.Sr.gameObject);
            _decals.Clear();
        }

        public int DecalCount => _decals.Count;

        public void Label(Vector2 at, string text, Color col, float size = 28f, float life = 1.3f)
        {
            if (Texts.Count > 40) return;
            Texts.Add(new FloatText { World = at + new Vector2(0, 1.2f), Text = text, Col = col, Max = life, Size = size, TargetId = -1 });
        }
    }
}
