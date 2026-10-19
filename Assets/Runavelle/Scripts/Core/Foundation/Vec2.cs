using System;

namespace Runavelle.Core
{
    /// <summary>Engine-free 2D vector used by the simulation. +X = east, +Y = north. 1 unit = 1 tile.</summary>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Y;

        public Vec2(float x, float y) { X = x; Y = y; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public float SqrMagnitude => X * X + Y * Y;
        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y);

        public Vec2 Normalized
        {
            get
            {
                float m = Magnitude;
                return m > 1e-6f ? new Vec2(X / m, Y / m) : Zero;
            }
        }

        public Vec2 ClampMagnitude(float max)
        {
            float m = Magnitude;
            return m > max && m > 1e-6f ? this * (max / m) : this;
        }

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;
        public static float SqrDistance(Vec2 a, Vec2 b) => (a - b).SqrMagnitude;
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        public static Vec2 MoveTowards(Vec2 current, Vec2 target, float maxDelta)
        {
            Vec2 d = target - current;
            float m = d.Magnitude;
            if (m <= maxDelta || m < 1e-6f) return target;
            return current + d * (maxDelta / m);
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.X / s, a.Y / s);

        public bool Equals(Vec2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => X.GetHashCode() * 397 ^ Y.GetHashCode();
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    /// <summary>Integer grid coordinate.</summary>
    [Serializable]
    public struct Int2 : IEquatable<Int2>
    {
        public int X;
        public int Y;

        public Int2(int x, int y) { X = x; Y = y; }

        public Vec2 Center => new Vec2(X + 0.5f, Y + 0.5f);

        public static Int2 FromPosition(Vec2 p) => new Int2((int)Math.Floor(p.X), (int)Math.Floor(p.Y));

        public static Int2 operator +(Int2 a, Int2 b) => new Int2(a.X + b.X, a.Y + b.Y);
        public static Int2 operator -(Int2 a, Int2 b) => new Int2(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Int2 a, Int2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Int2 a, Int2 b) => !(a == b);

        public int ChebyshevDistance(Int2 o) => Math.Max(Math.Abs(X - o.X), Math.Abs(Y - o.Y));

        public bool Equals(Int2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Int2 v && Equals(v);
        public override int GetHashCode() => X * 73856093 ^ Y * 19349663;
        public override string ToString() => $"[{X},{Y}]";
    }

    public static class MathUtil
    {
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
