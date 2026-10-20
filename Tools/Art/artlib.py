"""Runavelle procedural art library (Skia). Original storybook style: dark ink outline, soft cel shading,
warm highlights. Everything is generated from code in this repository - no external images.

Coordinates: pixels, origin top-left. All sprites are drawn at 1x final resolution (64 px per tile)."""
import json
import math
import os
import random

import skia

INK = (0x2B, 0x20, 0x2E)
# RUNAVELLE_ART_OUT lets Tools/ArtPipeline generate into a staging folder (transactional import), default = game folder
OUT_ROOT = os.environ.get("RUNAVELLE_ART_OUT") or os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Runavelle", "Resources", "Art")
TILE = 64
MANIFEST = {}


def rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def c4(col, a=255):
    return skia.Color(int(col[0]), int(col[1]), int(col[2]), int(a))


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def lighten(c, t):
    return mix(c, (255, 248, 230), t)


def darken(c, t):
    return mix(c, (30, 20, 45), t)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        # invisible safety margin: strokes/weapons drawn past the nominal canvas are kept and the saved
        # sprite grows to fit (see save / write_manifest), instead of being silently cropped
        self.pad = SAFE_PAD
        self.surface = skia.Surface(w + 2 * self.pad, h + 2 * self.pad)
        self.c = self.surface.getCanvas()
        self.c.clear(skia.ColorTRANSPARENT)
        self.c.translate(self.pad, self.pad)

    # ---------------------------------------------------------------- primitives
    def paint(self, col, a=255, stroke=0, aa=True, shader=None, blur=0):
        p = skia.Paint(AntiAlias=aa, Color=c4(col, a))
        if stroke:
            p.setStyle(skia.Paint.kStroke_Style)
            p.setStrokeWidth(stroke)
            p.setStrokeJoin(skia.Paint.kRound_Join)
            p.setStrokeCap(skia.Paint.kRound_Cap)
        if shader is not None:
            p.setShader(shader)
        if blur:
            p.setMaskFilter(skia.MaskFilter.MakeBlur(skia.kNormal_BlurStyle, blur))
        return p

    def vgrad(self, y0, y1, top, bottom):
        return skia.GradientShader.MakeLinear([skia.Point(0, y0), skia.Point(0, y1)], [c4(top), c4(bottom)])

    def rgrad(self, cx, cy, r, inner, outer, ai=255, ao=255):
        return skia.GradientShader.MakeRadial(skia.Point(cx, cy), r, [c4(inner, ai), c4(outer, ao)])

    def shape(self, path, fill, ink=3.0, shade=True, light_dir=(-0.6, -0.8), gradient=None, ink_col=INK):
        """Fill a path with cel shading and an ink outline."""
        b = path.getBounds()
        if gradient is not None:
            self.c.drawPath(path, self.paint(fill, shader=gradient))
        else:
            sh = self.vgrad(b.top(), b.bottom(), lighten(fill, 0.18), darken(fill, 0.22)) if shade else None
            self.c.drawPath(path, self.paint(fill, shader=sh))
        if shade:
            # soft rim light top-left
            self.c.save()
            self.c.clipPath(path, doAntiAlias=True)
            off = skia.Path(path)
            off.offset(-light_dir[0] * 3, -light_dir[1] * 3)
            hl = skia.Op(path, off, skia.PathOp.kDifference_PathOp) or skia.Path()
            self.c.drawPath(hl, self.paint(lighten(fill, 0.5), 150))
            self.c.restore()
        if ink:
            self.c.drawPath(path, self.paint(ink_col, stroke=ink))

    def rect(self, x, y, w, h, fill, r=4, ink=3.0, shade=True, gradient=None):
        p = skia.Path()
        p.addRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(x, y, w, h), r, r))
        self.shape(p, fill, ink, shade, gradient=gradient)
        return p

    def ellipse(self, cx, cy, rx, ry, fill, ink=3.0, shade=True, gradient=None):
        p = skia.Path()
        p.addOval(skia.Rect.MakeLTRB(cx - rx, cy - ry, cx + rx, cy + ry))
        self.shape(p, fill, ink, shade, gradient=gradient)
        return p

    def poly(self, pts, fill, ink=3.0, shade=True, close=True, gradient=None):
        p = skia.Path()
        p.moveTo(*pts[0])
        for q in pts[1:]:
            p.lineTo(*q)
        if close:
            p.close()
        self.shape(p, fill, ink, shade, gradient=gradient)
        return p

    def smooth(self, pts, fill, ink=3.0, shade=True, gradient=None):
        """Closed Catmull-Rom blob through points."""
        p = skia.Path()
        n = len(pts)
        p.moveTo(*pts[0])
        for i in range(n):
            p0, p1, p2, p3 = pts[(i - 1) % n], pts[i], pts[(i + 1) % n], pts[(i + 2) % n]
            c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
            c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
            p.cubicTo(*c1, *c2, *p2)
        p.close()
        self.shape(p, fill, ink, shade, gradient=gradient)
        return p

    def line(self, x0, y0, x1, y1, col, w=3, a=255):
        self.c.drawLine(x0, y0, x1, y1, self.paint(col, a, stroke=w))

    def inked_line(self, x0, y0, x1, y1, col, w=4):
        self.c.drawLine(x0, y0, x1, y1, self.paint(INK, stroke=w + 3))
        self.c.drawLine(x0, y0, x1, y1, self.paint(col, stroke=w))

    def circle(self, cx, cy, r, col, a=255, blur=0):
        self.c.drawCircle(cx, cy, r, self.paint(col, a, blur=blur))

    def shadow(self, cx, cy, rx, ry, a=90):
        self.c.drawOval(skia.Rect.MakeLTRB(cx - rx, cy - ry, cx + rx, cy + ry), self.paint((20, 15, 35), a, blur=2.5))

    def glow(self, cx, cy, r, col, a=200):
        self.c.drawCircle(cx, cy, r, self.paint(col, shader=self.rgrad(cx, cy, r, col, col, a, 0)))

    def noise(self, rng, count, col, a, size=(1, 2), area=None, clip=None):
        x0, y0, x1, y1 = area or (0, 0, self.w, self.h)
        if clip is not None:
            self.c.save()
            self.c.clipPath(clip, doAntiAlias=True)
        for _ in range(count):
            r = rng.uniform(*size)
            self.c.drawCircle(rng.uniform(x0, x1), rng.uniform(y0, y1), r, self.paint(col, a))
        if clip is not None:
            self.c.restore()

    def text(self, s, x, y, size, col):
        font = skia.Font(None, size)
        self.c.drawString(s, x, y, font, self.paint(col))

    def save(self, key, pivot=None, ppu=TILE):
        path = os.path.join(OUT_ROOT, key + ".png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        pad = getattr(self, "pad", 0)
        px, py = pivot if pivot else (self.w / 2, self.h / 2)
        arr = self.surface.makeImageSnapshot().toarray()
        _PENDING[key] = (arr, pad, self.w, self.h, px, py, ppu)
        _write_crop(key, _spill(key, arr, pad, self.w, self.h), _caller_module())
        return path


def _caller_module():
    """Provenance: the generator module that produced the sprite (first frame outside artlib)."""
    import inspect
    for fr in inspect.stack()[2:]:
        mod = os.path.splitext(os.path.basename(fr.filename))[0]
        if mod != "artlib":
            return "Tools/Art/" + mod + ".py"
    return "unknown"


SAFE_PAD = 64
EXPAND_PREFIXES = ("unit/", "enemy/", "general/", "building/", "decor/")  # sprites allowed to grow past their nominal canvas
_PENDING = {}


def _spill(key, arr, pad, w, h):
    """Extra pixels (l, t, r, b) needed so nothing opaque is cut, +2 px breathing room."""
    if not pad or not key.startswith(EXPAND_PREFIXES):
        return (0, 0, 0, 0)
    import numpy as np
    ys, xs = np.nonzero(arr[:, :, 3] > 0)
    if len(xs) == 0:
        return (0, 0, 0, 0)
    x0, x1, y0, y1 = xs.min() - pad, xs.max() - pad, ys.min() - pad, ys.max() - pad
    g = lambda v: 0 if v < 0 else min(pad, int(v) + 2)  # v == 0: touching the edge also counts
    return (g(-x0), g(-y0), g(x1 - (w - 1)), g(y1 - (h - 1)))


def _write_crop(key, spill, src):
    import numpy as np
    arr, pad, w, h, px, py, ppu = _PENDING[key]
    l, t, r, b = spill
    crop = np.ascontiguousarray(arr[pad - t:pad + h + b, pad - l:pad + w + r])
    W, H = w + l + r, h + t + b
    skia.Image.fromarray(crop, colorType=skia.kBGRA_8888_ColorType, alphaType=skia.kUnpremul_AlphaType).save(os.path.join(OUT_ROOT, key + ".png"), skia.kPNG)
    MANIFEST[key] = {"w": W, "h": H, "px": round((px + l) / W, 4), "py": round(1 - (py + t) / H, 4), "ppu": ppu, "src": src}


def _frame_base(key):
    head, _, tail = key.rpartition("_")
    return head if tail.isdigit() and key.startswith(("unit/", "enemy/", "general/")) else key


def _unify_frames():
    """Frames of one animation must share size + pivot: grow every frame by the group's max spill."""
    groups = {}
    for k in _PENDING:
        groups.setdefault(_frame_base(k), []).append(k)
    for base, keys in groups.items():
        if len(keys) < 2:
            continue
        sp = [_spill(k, *_PENDING[k][:4]) for k in keys]
        m = tuple(max(s[i] for s in sp) for i in range(4))
        for k, s in zip(keys, sp):
            if s != m:
                _write_crop(k, m, MANIFEST[k]["src"])


def write_manifest():
    _unify_frames()
    path = os.path.join(OUT_ROOT, "manifest.json")
    with open(path, "w") as f:
        json.dump(MANIFEST, f, indent=0, sort_keys=True)
    return path


def star(cx, cy, r1, r2, n=5, rot=-math.pi / 2):
    pts = []
    for i in range(n * 2):
        r = r1 if i % 2 == 0 else r2
        a = rot + i * math.pi / n
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts
