using System.Collections.Generic;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>
    /// Lightweight pooled sprite particles + aggregated floating numbers/labels. Quality level scales particle
    /// counts only; nothing here affects the simulation.
    /// </summary>
    public sealed class Vfx
    {
        private sealed class P
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel;
            public float Life, Max, Size, EndSize, Spin, Gravity;
            public Color Col;
            public bool Active;
        }

        public sealed class FloatText
        {
            public Vector2 World;
            public string Text;
            public Color Col;
            public float Life, Max, Size;
            public int TargetId;
            public float Value;
        }

        private readonly Transform _root;
        private readonly List<P> _ps = new List<P>();
        public readonly List<FloatText> Texts = new List<FloatText>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private int _level = 2;
        public int ActiveCount { get; private set; }

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

        private P Emit(string sprite, Vector2 pos, Vector2 vel, float life, float size, float endSize, Color col, int order = 30500, float gravity = 0f)
        {
            int cap = _level == 0 ? 120 : _level == 1 ? 300 : 600;
            if (ActiveCount >= cap) return null;
            P p = null;
            foreach (var q in _ps) if (!q.Active) { p = q; break; }
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
            p.Pos = pos; p.Vel = vel; p.Life = 0; p.Max = life; p.Size = size; p.EndSize = endSize; p.Col = col; p.Gravity = gravity;
            p.Spin = Random.Range(-180f, 180f);
            ActiveCount++;
            return p;
        }

        public void Update(float dt, GameSettings s)
        {
            _level = s.VfxLevel;
            ActiveCount = 0;
            foreach (var p in _ps)
            {
                if (!p.Active) continue;
                p.Life += dt;
                if (p.Life >= p.Max) { p.Active = false; p.Sr.gameObject.SetActive(false); continue; }
                ActiveCount++;
                p.Vel.y -= p.Gravity * dt;
                p.Pos += p.Vel * dt;
                float t = p.Life / p.Max;
                float sz = Mathf.Lerp(p.Size, p.EndSize, t);
                p.Sr.transform.position = new Vector3(p.Pos.x, p.Pos.y, 0);
                p.Sr.transform.localScale = new Vector3(sz, sz, 1);
                p.Sr.transform.rotation = Quaternion.Euler(0, 0, p.Spin * p.Life);
                var c = p.Col;
                c.a *= 1f - t * t;
                p.Sr.color = c;
            }
            for (int i = Texts.Count - 1; i >= 0; i--)
            {
                var ft = Texts[i];
                ft.Life += dt;
                ft.World.y += dt * 0.6f;
                if (ft.Life >= ft.Max) Texts.RemoveAt(i);
            }
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

        public void Trail(Vector2 at, Color col)
        {
            Emit("fx/glow", at, Vector2.zero, 0.25f, 0.25f, 0.05f, new Color(col.r, col.g, col.b, 0.6f), 29900);
        }

        public void ZoneMote(Vector2 at, float r, Color col)
        {
            var p = at + Random.insideUnitCircle * r;
            Emit("fx/spark", p, new Vector2(0, Random.Range(0.3f, 0.8f)), 0.6f, 0.15f, 0.02f, new Color(col.r, col.g, col.b, 0.8f), 30200);
        }

        public void Slash(Vector2 at, float facing, Color col)
        {
            var p = Emit("fx/slash", at + new Vector2(facing * 0.45f, 0.45f), Vector2.zero, 0.18f, 0.6f, 0.9f, col, 30600);
            if (p != null) { p.Spin = 0; p.Sr.flipX = facing < 0; }
        }

        public void Element(Vector2 at, ElementId e, float scale = 1f)
        {
            var c = ArtLibrary.ElementColor(e);
            switch (e)
            {
                case ElementId.Fire: Burst(at, c, 8, 1.8f, 0.4f * scale, "fx/glow"); Smoke(at, 2, new Color(0.4f, 0.3f, 0.3f, 0.5f)); break;
                case ElementId.Water: Burst(at, c, 8, 2.2f, 0.25f * scale, "fx/orb"); break;
                case ElementId.Lightning: Burst(at, c, 6, 3.5f, 0.35f * scale, "fx/spark"); break;
                case ElementId.Ice: Burst(at, c, 8, 1.5f, 0.3f * scale, "fx/spark"); break;
                case ElementId.Wind: Ring(at + new Vector2(0, 0.3f), c, 0.6f * scale, 0.35f); break;
                case ElementId.Earth: Burst(at, c, 6, 2f, 0.3f * scale, "fx/rock"); break;
                case ElementId.Light: Burst(at, c, 8, 1.5f, 0.35f * scale, "fx/spark"); Ring(at + new Vector2(0, 0.3f), c, 0.4f * scale, 0.3f); break;
                case ElementId.Shadow: Burst(at, c, 6, 1.2f, 0.45f * scale, "fx/smoke"); break;
                default: Burst(at, new Color(1, 1, 0.9f), 4, 1.5f, 0.2f * scale); break;
            }
        }

        /// <summary>Adds or merges a floating number above a target (damage aggregation: one number per target per 0.35s).</summary>
        public void Number(Vector2 at, int targetId, float value, Color col, float size = 26f)
        {
            foreach (var t in Texts)
                if (t.TargetId == targetId && targetId >= 0 && t.Life < 0.35f && t.Col == col)
                {
                    t.Value += value;
                    t.Text = Mathf.RoundToInt(t.Value).ToString();
                    t.Life = Mathf.Min(t.Life, 0.1f);
                    t.Size = Mathf.Min(size * 1.6f, size + t.Value * 0.05f);
                    return;
                }
            if (Texts.Count > 80) return;
            Texts.Add(new FloatText { World = at + new Vector2(Random.Range(-0.2f, 0.2f), 0.9f), Text = Mathf.RoundToInt(value).ToString(), Col = col, Max = 0.9f, Size = size, TargetId = targetId, Value = value });
        }

        public void Label(Vector2 at, string text, Color col, float size = 28f, float life = 1.3f)
        {
            if (Texts.Count > 80) return;
            Texts.Add(new FloatText { World = at + new Vector2(0, 1.2f), Text = text, Col = col, Max = life, Size = size, TargetId = -1 });
        }
    }
}
