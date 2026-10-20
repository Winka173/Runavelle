"""Blender headless 3D -> 2D renders for Runavelle (run via `artpipe.py render`).

    blender -b --factory-startup --python Tools/ArtPipeline/blender/render_assets.py -- --out Build/art_staging [--only KEY ...]

Writes 2x-resolution renders to <out>/_raw/<key>.png plus <out>/_raw/render_manifest.json
(canvas size + pivot at 1x). `artpipe.py render` downsamples them (premultiplied Lanczos) into
<out>/<key>.png and merges the manifest.

Camera contract (Docs/Enhancement/ART_BIBLE.md): orthographic, elevation 35 deg, azimuth 0 (the game
grid is axis-aligned), ground depth stretched by 1/sin(35) so a 1x1 tile footprint reads as a
64x64 px square; key light upper-left, cool fill, warm rim; ink outline #2b2233 (Freestyle);
contact shadow from a shadow-catcher plane. Cycles CPU, denoiser off (build has no OIDN).
"""
import json
import math
import os
import sys

import bpy

EL = math.radians(35.0)
DEPTH = 1.0 / math.sin(EL)
PPU = 64
SCALE = 2  # render at 2x, downsampled by artpipe (draft quality: 1x)
SAMPLES = 96
BUILDING_SUN_TILT = 20  # degrees from zenith; town buildings use 12 for short contact shadows
CATCHER = (4.0, 3.9)  # shadow-catcher size in tiles (set per asset from its footprint)


def srgb(h):
    h = h.lstrip('#')
    c = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return tuple(((v + 0.055) / 1.055) ** 2.4 if v > 0.04045 else v / 12.92 for v in c) + (1.0,)


# ------------------------------------------------------------------------------------------------ scene
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    s.render.engine = 'CYCLES'
    s.cycles.device = 'CPU'
    s.cycles.samples = SAMPLES
    s.cycles.use_adaptive_sampling = True
    s.cycles.adaptive_threshold = 0.02
    s.cycles.use_denoising = False
    s.cycles.max_bounces = 4
    s.cycles.diffuse_bounces = 2
    s.cycles.glossy_bounces = 1
    s.cycles.transmission_bounces = 2
    s.cycles.pixel_filter_type = 'BLACKMAN_HARRIS'
    s.cycles.filter_width = 1.5
    s.render.film_transparent = True
    s.render.image_settings.file_format = 'PNG'
    s.render.image_settings.color_mode = 'RGBA'
    s.render.image_settings.color_depth = '8'
    s.view_settings.view_transform = 'Standard'
    s.view_settings.look = 'None'
    s.render.threads_mode = 'AUTO'
    for vl in s.view_layers:
        vl.cycles.use_denoising = False
    # world: soft cool sky fill
    w = bpy.data.worlds.new("sky")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = srgb('#b8c8e8')
    bg.inputs[1].default_value = 0.0  # no sky light: its occlusion made the shadow catcher paint grey boxes
    s.world = w
    # cool fill from the camera side, shadowless: replaces the sky ambient without touching the catcher
    fill = bpy.data.lights.new("fill", 'SUN')
    fill.energy = 1.1
    fill.color = srgb('#c8d4f0')[:3]
    fill.use_shadow = False
    if hasattr(fill, "cycles"):
        fill.cycles.cast_shadow = False
    fo = bpy.data.objects.new("fill", fill)
    fo.rotation_euler = (math.radians(50), 0, math.radians(10))
    s.collection.objects.link(fo)
    top = bpy.data.lights.new("top", 'SUN')
    top.energy = 0.6
    top.color = srgb('#e8ecff')[:3]
    if hasattr(top, "cycles"):
        top.cycles.cast_shadow = False
    top.use_shadow = False
    to = bpy.data.objects.new("top", top)
    s.collection.objects.link(to)
    # key (upper-left, front), warm rim (back-right)
    key = bpy.data.lights.new("key", 'SUN')
    key.energy = 3.4
    key.angle = math.radians(14)  # softer contact shadows
    key.color = srgb('#fff1dc')[:3]
    ko = bpy.data.objects.new("key", key)
    ko.rotation_euler = (math.radians(BUILDING_SUN_TILT), 0, math.radians(-35))
    s.collection.objects.link(ko)
    rim = bpy.data.lights.new("rim", 'SUN')
    rim.energy = 1.2
    rim.color = srgb('#ffd2a0')[:3]
    rim.use_shadow = False  # rim is a grazing edge light: its long shadows read as dirt on the ground (EEVEE flag)
    if hasattr(rim, "cycles"):
        rim.cycles.cast_shadow = False  # the flag Cycles actually honours
    ro = bpy.data.objects.new("rim", rim)
    ro.rotation_euler = (math.radians(70), 0, math.radians(150))
    s.collection.objects.link(ro)
    # collections: assets (outlined) / helpers (not outlined)
    assets = bpy.data.collections.new("assets")
    helpers = bpy.data.collections.new("helpers")
    s.collection.children.link(assets)
    s.collection.children.link(helpers)
    root = bpy.data.objects.new("ROOT", None)
    # depth stretch is applied to positions / box depths by Kit (round shapes keep their true form)
    assets.objects.link(root)
    # shadow catcher
    bpy.ops.mesh.primitive_plane_add(size=1)
    sc = bpy.context.active_object
    sc.scale = (CATCHER[0], CATCHER[1] * DEPTH, 1)
    for c in list(sc.users_collection):
        c.objects.unlink(sc)
    helpers.objects.link(sc)
    sc.is_shadow_catcher = True
    # freestyle ink outline (assets collection only)
    s.render.use_freestyle = True
    s.render.line_thickness_mode = 'ABSOLUTE'
    s.render.line_thickness = 1.1 * SCALE
    vl = s.view_layers[0]
    vl.use_freestyle = True
    fs = vl.freestyle_settings
    fs.crease_angle = math.radians(120)
    ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("ink")
    ls.select_by_collection = True
    ls.collection = assets
    ls.select_silhouette = True
    ls.select_border = True
    ls.select_crease = True
    ls.select_external_contour = True
    if ls.linestyle is None:
        ls.linestyle = bpy.data.linestyles.new("ink")
    ls.linestyle.color = srgb('#2b2233')[:3]
    ls.linestyle.thickness = 1.1 * SCALE
    return s, assets, root


_mats = {}


def mat(name, color, rough=0.85, emit=None, emit_strength=0.0, brick=None):
    key = (name, color, emit, brick)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = srgb(color)
    p.inputs["Roughness"].default_value = rough
    if "Specular IOR Level" in p.inputs:
        p.inputs["Specular IOR Level"].default_value = 0.25
    if brick:
        # storybook masonry: soft brick pattern from object coordinates
        tc = nt.nodes.new("ShaderNodeTexCoord")
        b = nt.nodes.new("ShaderNodeTexBrick")
        b.inputs["Color1"].default_value = srgb(color)
        b.inputs["Color2"].default_value = srgb(brick)
        b.inputs["Mortar"].default_value = srgb(brick)
        b.inputs["Scale"].default_value = 3.2
        b.inputs["Mortar Size"].default_value = 0.025
        b.inputs["Brick Width"].default_value = 0.6
        b.inputs["Row Height"].default_value = 0.28
        nt.links.new(tc.outputs["Object"], b.inputs["Vector"])
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type = 'RGBA'
        mix.inputs[0].default_value = 0.45
        mix.inputs[6].default_value = srgb(color)
        nt.links.new(b.outputs["Color"], mix.inputs[7])
        nt.links.new(mix.outputs[2], p.inputs["Base Color"])
    if emit:
        p.inputs["Emission Color"].default_value = srgb(emit)
        p.inputs["Emission Strength"].default_value = emit_strength
    _mats[key] = m
    return m


# ------------------------------------------------------------------------------------------------ primitives
class Kit:
    def __init__(self, coll, root):
        self.coll, self.root = coll, root

    def _own(self, o, m):
        for c in list(o.users_collection):
            c.objects.unlink(o)
        self.coll.objects.link(o)
        o.parent = self.root
        if m is not None:
            o.data.materials.append(m)
        return o

    def mesh(self, verts, faces, m, name="mesh", smooth=False):
        """Custom mesh from world-space-ish verts (y is depth in tiles, scaled here like every other primitive)."""
        me = bpy.data.meshes.new(name)
        me.from_pydata([(x, y * DEPTH, z) for x, y, z in verts], [], faces)
        me.update()
        o = bpy.data.objects.new(name, me)
        self.coll.objects.link(o)
        o.parent = self.root
        if m is not None:
            o.data.materials.append(m)
        if smooth:
            for poly in me.polygons:
                poly.use_smooth = True
        return o

    def gable(self, x, y, z, w, d, h, m, overhang=0.06, ridge="y"):
        """Gable roof. ridge="y" (default) runs the ridge into the screen so the triangular gable end faces the camera —
        the readable storybook-house silhouette; ridge="x" shows a long slope instead."""
        x0, x1, y0, y1 = x - w / 2 - overhang, x + w / 2 + overhang, y - d / 2 - overhang, y + d / 2 + overhang
        if ridge == "x":
            v = [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z), (x0, y, z + h), (x1, y, z + h)]
            f = [(0, 1, 5, 4), (2, 3, 4, 5), (0, 4, 3), (1, 2, 5), (0, 3, 2, 1)]
        else:
            v = [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z), (x, y0, z + h), (x, y1, z + h)]
            f = [(0, 4, 5, 3), (1, 2, 5, 4), (0, 1, 4), (3, 5, 2), (0, 3, 2, 1)]
        return self.mesh(v, f, m, "gable")

    def hip(self, x, y, z, w, d, h, m, overhang=0.06):
        """Four-sided pyramid roof over a w x d footprint."""
        x0, x1, y0, y1 = x - w / 2 - overhang, x + w / 2 + overhang, y - d / 2 - overhang, y + d / 2 + overhang
        v = [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z), (x, y, z + h)]
        f = [(0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4), (0, 3, 2, 1)]
        return self.mesh(v, f, m, "hip")

    def holdout_box(self, x0, x1, y0, y1, z0, z1):
        """Invisible occluder: masks pixels and Freestyle lines behind it (seamless joins at tile borders)."""
        bpy.ops.mesh.primitive_cube_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2 * DEPTH, (z0 + z1) / 2))
        o = bpy.context.active_object
        o.scale = (x1 - x0, (y1 - y0) * DEPTH, z1 - z0)
        for c in list(o.users_collection):
            c.objects.unlink(o)
        bpy.data.collections["helpers"].objects.link(o)
        # a Holdout *shader* on ordinary geometry: transparent pixels AND a real occluder for Freestyle
        # (object-level is_holdout hides pixels but Freestyle still draws the lines behind it)
        m = bpy.data.materials.new("holdout")
        m.use_nodes = True
        nt = m.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        hn = nt.nodes.new("ShaderNodeHoldout")
        nt.links.new(hn.outputs[0], out.inputs["Surface"])
        o.data.materials.append(m)
        o.visible_shadow = False
        return o

    def bevel(self, o, w=0.025):
        b = o.modifiers.new("bevel", 'BEVEL')
        b.width = w
        b.segments = 2
        b.limit_method = 'ANGLE'
        return o

    def box(self, x, y, z, w, d, h, m, bevel=0.025):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y * DEPTH, z + h / 2))
        o = bpy.context.active_object
        o.scale = (w, d * DEPTH, h)
        self._own(o, m)
        if bevel:
            self.bevel(o, bevel)
        return o

    def cyl(self, x, y, z, r, h, m, verts=24):
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=(x, y * DEPTH, z + h / 2))
        o = bpy.context.active_object
        bpy.ops.object.shade_smooth()
        return self.bevel(self._own(o, m), 0.015)

    def cone(self, x, y, z, r, h, m, verts=24, r2=0.0):
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=(x, y * DEPTH, z + h / 2))
        o = bpy.context.active_object
        bpy.ops.object.shade_smooth()
        return self._own(o, m)

    def pyramid(self, x, y, z, w, d, h, m):
        o = self.cone(x, y, z, 0.7071, h, m, verts=4)
        o.rotation_euler = (0, 0, math.radians(45))
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        o.scale = (w, d * DEPTH, 1)
        bpy.ops.object.shade_flat()
        return o

    def gem(self, x, y, z, r, m):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=r, location=(x, y * DEPTH, z))
        o = bpy.context.active_object
        o.scale = (0.7, 0.7, 1.5)
        return self._own(o, m)

    def torus(self, x, y, z, r, t, m):
        bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=t, location=(x, y * DEPTH, z), major_segments=48, minor_segments=8)
        o = bpy.context.active_object
        bpy.ops.object.shade_smooth()
        return self._own(o, m)

    # -------------------------------------------------------------------------------------- composites
    def crenels_line(self, x0, y0, x1, y1, z, m, size=0.13, gap=0.11, depth=None):
        L = math.hypot(x1 - x0, y1 - y0)
        n = max(1, int((L + gap) / (size + gap)))
        for i in range(n):
            t = (i + 0.5) / n
            cx, cy = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            along_x = abs(x1 - x0) >= abs(y1 - y0)
            dd = depth or size
            self.box(cx, cy, z, size if along_x else dd, dd if along_x else size, size * 1.1, m, bevel=0.01)

    def crenels_ring(self, cx, cy, r, z, m, n=8, size=0.12):
        for i in range(n):
            a = 2 * math.pi * i / n
            self.box(cx + math.cos(a) * (r - size * 0.4), cy + math.sin(a) * (r - size * 0.4), z, size, size, size * 1.1, m, bevel=0.01)

    def wall(self, x0, y0, x1, y1, z, h, t, stone, top, cren=True):
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        w = abs(x1 - x0) + (t if y0 == y1 else 0)
        d = abs(y1 - y0) + (t if x0 == x1 else 0)
        self.box(cx, cy, z, max(w, t), max(d, t), h, stone)
        self.box(cx, cy, z + h, max(w, t) + 0.04, max(d, t) + 0.04, 0.06, top, bevel=0.01)
        if cren:
            if y0 == y1:
                self.crenels_line(x0, y0 - t * 0.3, x1, y1 - t * 0.3, z + h + 0.06, top, depth=t * 0.4)
            else:
                self.crenels_line(x0 - t * 0.3, y0, x1 - t * 0.3, y1, z + h + 0.06, top, depth=t * 0.4)

    def tower(self, x, y, r, h, stone, top, roof=None, roof_h=0.7, cren=True):
        self.cyl(x, y, 0, r, h, stone)
        self.cyl(x, y, h, r + 0.04, 0.07, top)
        if roof is not None:
            self.cone(x, y, h + 0.07, r + 0.1, roof_h, roof)
            self.cyl(x, y, h + 0.07 + roof_h - 0.02, 0.015, 0.22, mat("pole", '#5a4636'), verts=6)
        elif cren:
            self.crenels_ring(x, y, r + 0.03, h + 0.07, top, n=max(6, int(r * 22)))
        # arrow slit facing the camera
        self.box(x, y - (r - 0.01) / DEPTH, h * 0.55, 0.05, 0.04, 0.2, mat("slit", '#2a2433', rough=1.0), bevel=0)

    def flag(self, x, y, z, color, w=0.32, h=0.2, pole=0.55):
        self.cyl(x, y, z, 0.018, pole, mat("pole", '#5a4636'), verts=6)
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x + w / 2 + 0.01, y * DEPTH, z + pole - h / 2 - 0.02))
        o = bpy.context.active_object
        o.rotation_euler = (math.radians(90), 0, 0)
        o.scale = (w, h, 1)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        sub = o.modifiers.new("sub", 'SUBSURF')
        sub.levels = 2
        wave = o.modifiers.new("wave", 'WAVE')
        wave.use_normal = False
        wave.height = 0.03
        wave.width = 0.25
        wave.narrowness = 1.2
        self._own(o, mat("flag" + color, color, rough=0.9))
        return o

    def banner(self, x, y, z, color, w=0.2, h=0.42, trim='#e8c25a'):
        """Hanging banner on a wall face (front)."""
        self.box(x, y, z - h, w, 0.03, h, mat("ban" + color, color, rough=0.9), bevel=0.005)
        self.box(x, y - 0.01, z - h - 0.02, w * 0.5, 0.03, 0.06, mat("trim" + trim, trim, rough=0.5), bevel=0.005)

    def door(self, x, y, z, w, h, wood='#7a5236'):
        self.box(x, y, z, w + 0.08, 0.06, h + 0.06, mat("doorframe", '#6f6a80'), bevel=0.01)
        self.box(x, y - 0.03, z, w, 0.04, h, mat("door" + wood, wood, rough=0.9), bevel=0.005)
        self.cyl(x, y - 0.04, z + h - 0.02, w / 2, 0.04, mat("door" + wood, wood, rough=0.9)).rotation_euler = (math.radians(90), 0, 0)

    def window(self, x, y, z, w=0.09, h=0.16, lit=False):
        m = mat("winlit", '#ffd77a', emit='#ffc85a', emit_strength=1.2) if lit else mat("win", '#2c2638', rough=1.0)
        self.box(x, y, z, w, 0.03, h, m, bevel=0)


# ------------------------------------------------------------------------------------------------ palettes
PAL = {
    '1': dict(stone='#b8b0a4', brick='#8f877c', top='#cfc8bc', roof='#4e74c4', accent='#e8c25a', flag='#d24b4b', plinth='#8a8a6a'),
    '2': dict(stone='#aaa6b4', brick='#7f7b8c', top='#c8c4d2', roof='#3f62b0', accent='#e8c25a', flag='#d24b4b', plinth='#7e7f72'),
    '3a': dict(stone='#a7a3b6', brick='#76728a', top='#bcb7cc', roof='#2f4f9a', accent='#f0c84a', flag='#2f4f9a', plinth='#787682'),
    '3b': dict(stone='#d6cbe6', brick='#a99bc0', top='#ece4f6', roof='#7a4fc4', accent='#8fe3ff', flag='#7a4fc4', plinth='#8f86a6'),
}


def castle(k, lvl):
    p = PAL[lvl]
    stone = mat("stone" + lvl, p['stone'], brick=p['brick'])
    top = mat("top" + lvl, p['top'])
    roof = mat("roof" + lvl, p['roof'], rough=0.6)
    gold = mat("gold", p['accent'], rough=0.35)
    plinth = mat("plinth" + lvl, p['plinth'], rough=0.95)
    k.box(0, 0, 0, 2.86, 2.86, 0.1, plinth, bevel=0.04)
    if lvl == '1':
        # modest hold: keep + two flanking towers, palisade-free
        k.box(0, 0.25, 0.1, 1.5, 1.15, 1.35, stone)
        k.wall(-0.75, -0.33, 0.75, -0.33, 1.45, 0.0, 0.0, stone, top, cren=False)
        k.crenels_line(-0.7, -0.27, 0.7, -0.27, 1.45, top)
        k.crenels_line(-0.7, 0.77, 0.7, 0.77, 1.45, top)
        k.pyramid(0, 0.25, 1.45, 1.1, 0.8, 0.55, roof)
        for sx in (-1, 1):
            k.tower(sx * 0.95, -0.45, 0.33, 1.75, stone, top, roof=roof, roof_h=0.7)
        k.door(0, -0.33, 0.1, 0.34, 0.5)
        k.window(-0.4, -0.34, 0.85)
        k.window(0.4, -0.34, 0.85)
        k.flag(0, 0.25, 2.0, p['flag'])
    elif lvl == '2':
        # walled castle: curtain wall + four roofed towers + tall keep
        k.box(0, 0.35, 0.1, 1.25, 1.0, 1.95, stone)
        k.crenels_line(-0.58, -0.12, 0.58, -0.12, 2.05, top)
        k.pyramid(0, 0.35, 2.05, 0.95, 0.75, 0.75, roof)
        k.window(-0.3, -0.16, 1.3, lit=True)
        k.window(0.3, -0.16, 1.3, lit=True)
        for (x0, y0, x1, y1) in ((-1.1, -1.05, -0.3, -1.05), (0.3, -1.05, 1.1, -1.05), (-1.1, 1.05, 1.1, 1.05),
                                 (-1.1, -1.05, -1.1, 1.05), (1.1, -1.05, 1.1, 1.05)):
            k.wall(x0, y0, x1, y1, 0.1, 0.7, 0.22, stone, top)
        # gatehouse
        k.box(0, -1.05, 0.1, 0.62, 0.34, 0.95, stone)
        k.crenels_line(-0.28, -1.17, 0.28, -1.17, 1.05, top)
        k.door(0, -1.22, 0.1, 0.32, 0.48)
        for sx in (-1, 1):
            for sy in (-1, 1):
                k.tower(sx * 1.1, sy * 1.05, 0.3, 1.3, stone, top, roof=roof, roof_h=0.62)
        k.banner(-0.2, -1.23, 0.95, p['flag'], w=0.12, h=0.3)
        k.banner(0.2, -1.23, 0.95, p['flag'], w=0.12, h=0.3)
        k.flag(0, 0.35, 2.75, p['flag'])
    elif lvl == '3a':
        # royal fortress: thick bastion ring, squat crenellated bastions, gold-crowned donjon
        for (x0, y0, x1, y1) in ((-1.1, -1.08, -0.36, -1.08), (0.36, -1.08, 1.1, -1.08), (-1.1, 1.08, 1.1, 1.08),
                                 (-1.1, -1.08, -1.1, 1.08), (1.1, -1.08, 1.1, 1.08)):
            k.wall(x0, y0, x1, y1, 0.1, 0.9, 0.3, stone, top)
        k.box(0, 0.3, 0.1, 1.45, 1.15, 2.05, stone)
        k.crenels_line(-0.68, -0.29, 0.68, -0.29, 2.15, top)
        k.cyl(0, 0.35, 2.15, 0.36, 0.75, stone)
        k.cyl(0, 0.35, 2.9, 0.4, 0.06, gold)
        k.cone(0, 0.35, 2.96, 0.46, 0.8, roof)
        k.gem(0, 0.35, 3.85, 0.07, gold)
        for x in (-0.45, 0, 0.45):
            k.window(x, -0.31, 1.35, lit=True)
        k.box(0, -1.08, 0.1, 0.78, 0.44, 1.2, stone)
        k.crenels_line(-0.36, -1.24, 0.36, -1.24, 1.3, top)
        k.door(0, -1.31, 0.1, 0.36, 0.56, wood='#5f4630')
        k.box(0, -1.32, 0.78, 0.5, 0.03, 0.06, gold, bevel=0)
        for sx in (-1, 1):
            for sy in (-1, 1):
                k.tower(sx * 1.1, sy * 1.08, 0.42, 1.25, stone, top, roof=None)
        for sx in (-1, 1):
            k.banner(sx * 0.62, -1.24, 0.95, p['flag'], w=0.2, h=0.5, trim=p['accent'])
            k.flag(sx * 1.1, -1.08, 1.4, p['accent'], w=0.28)
    else:  # 3b
        # arcane citadel: pale stone, slender spires, central spire crowned by a floating crystal
        rune = mat("rune", '#bfa6ff', emit='#a77bff', emit_strength=3.0)
        crystal = mat("crystal", '#8fe3ff', rough=0.15, emit='#6fd6ff', emit_strength=2.0)
        for (x0, y0, x1, y1) in ((-1.05, -1.05, -0.3, -1.05), (0.3, -1.05, 1.05, -1.05), (-1.05, 1.05, 1.05, 1.05),
                                 (-1.05, -1.05, -1.05, 1.05), (1.05, -1.05, 1.05, 1.05)):
            k.wall(x0, y0, x1, y1, 0.1, 0.55, 0.18, stone, top)
        k.box(0, 0.3, 0.1, 1.2, 1.0, 1.5, stone)
        k.crenels_line(-0.55, -0.2, 0.55, -0.2, 1.6, top)
        k.cyl(0, 0.35, 1.6, 0.42, 1.2, stone)
        k.torus(0, 0.35, 2.3, 0.44, 0.03, rune)
        k.cone(0, 0.35, 2.8, 0.52, 0.95, roof)
        k.gem(0, 0.35, 4.05, 0.17, crystal)
        k.window(-0.3, -0.21, 1.0, lit=True)
        k.window(0.3, -0.21, 1.0, lit=True)
        k.box(0, -1.05, 0.1, 0.6, 0.3, 0.8, stone)
        k.door(0, -1.21, 0.1, 0.3, 0.44, wood='#5a3f7a')
        k.box(0, -1.22, 0.62, 0.16, 0.02, 0.1, rune, bevel=0)
        for sx in (-1, 1):
            for sy in (-1, 1):
                k.tower(sx * 1.05, sy * 1.05, 0.2, 1.75 if sy < 0 else 2.0, stone, top, roof=roof, roof_h=0.85)
        k.torus(0, 0, 0.12, 1.32, 0.02, rune)


def kingdom(k, lvl, kd):
    """Kingdom evolution dressing on top of any castle tier (replaces the old overlay props)."""
    p = PAL[lvl]
    if kd == 'fortress':
        iron = mat("iron", '#6c6f7e', rough=0.45)
        stone = mat("stone" + lvl, p['stone'], brick=p['brick'])
        top = mat("top" + lvl, p['top'])
        # forward barbican: twin squat bastions + iron-banded portcullis
        for sx in (-1, 1):
            k.tower(sx * 0.55, -1.4, 0.24, 0.85, stone, top, roof=None)
        k.box(0, -1.42, 0.1, 0.62, 0.12, 0.62, iron, bevel=0.01)
        for i in range(4):
            k.box(-0.21 + i * 0.14, -1.49, 0.1, 0.03, 0.02, 0.6, mat("ink", '#3a3442'), bevel=0)
        k.box(0, -1.45, 0.72, 1.2, 0.18, 0.1, top, bevel=0.01)
        for sx in (-1, 1):
            k.box(sx * 1.38, 0.0, 0.1, 0.14, 2.3, 0.35, stone)
    elif kd == 'legion':
        red = '#c8363a'
        for sx in (-1, 1):
            k.flag(sx * 1.32, -1.32, 0.1, red, w=0.36, h=0.26, pole=1.45)
        # weapon rack + training dummy in the forecourt
        wood = mat("rackwood", '#7a5236')
        k.box(-0.75, -1.42, 0.1, 0.42, 0.05, 0.05, wood, bevel=0)
        for i in range(4):
            k.cyl(-0.9 + i * 0.1, -1.42, 0.1, 0.012, 0.42, mat("spear", '#9a9aa8'), verts=6)
            k.cone(-0.9 + i * 0.1, -1.42, 0.52, 0.025, 0.07, mat("tip", '#d8d8e2', rough=0.3), verts=6)
        k.cyl(0.78, -1.4, 0.1, 0.03, 0.42, wood, verts=8)
        k.box(0.78, -1.4, 0.38, 0.32, 0.04, 0.04, wood, bevel=0)
        k.gem(0.78, -1.4, 0.58, 0.08, mat("straw", '#d8b46a'))
        for sx in (-1, 1):
            k.banner(sx * 0.45, -1.06 - (0.3 if lvl == '1' else 0), 0.95 if lvl != '1' else 0.6, red, w=0.16, h=0.36)
    elif kd == 'arcanum':
        crystal = mat("acrystal", '#c9a6ff', rough=0.15, emit='#b07cff', emit_strength=1.6)
        rune = mat("rune", '#bfa6ff', emit='#a77bff', emit_strength=3.0)
        for i, (x, y, z) in enumerate(((-1.35, -0.6, 1.5), (1.35, -0.6, 1.7), (0.0, 1.3, 2.6))):
            k.gem(x, y, z, 0.11, crystal)
            k.torus(x, y, 0.12, 0.16, 0.015, rune)
        k.torus(0, 0, 0.13, 1.42, 0.022, rune)


# ------------------------------------------------------------------------------------------------ registry
def castle_spec(lvl, kd=None):
    key = "building/castle_" + lvl + ("_" + kd if kd else "")

    def build(kit):
        castle(kit, lvl)
        if kd:
            kingdom(kit, lvl, kd)
    # 4 x 6 tiles canvas; footprint front edge 0.25 tile above the bottom
    return key, dict(w=256, h=384, px=0.5, py=1.75 / 6.0, build=build)


ASSETS = {}
for _lvl in ('1', '2', '3a', '3b'):
    for _kd in (None, 'fortress', 'legion', 'arcanum'):
        _k, _s = castle_spec(_lvl, _kd)
        ASSETS[_k] = _s


# ------------------------------------------------------------------------------------------------ town buildings (2x2)
ELEM = {'fire': '#ff7a2e', 'water': '#3f9bff', 'lightning': '#ffe14a', 'ice': '#9ff0ff', 'wind': '#8ff0b0',
        'earth': '#c9965a', 'light': '#fff1a8', 'shadow': '#a35cf0', 'none': '#d9d4cc'}
STONE_C, STONE_B, TOP_C = '#cfc5b2', '#9c907e', '#ded6c6'
WOOD_C = '#a8733f'


def plinth(k, size, col='#8c8a76'):
    # low, inset foundation: grounds the building without painting the whole footprint (terrain stays visible)
    k.box(0, 0.05, 0, size - 0.75, size - 0.8, 0.035, mat("plinth" + col, col, rough=0.95), bevel=0.015)


def element_topper(k, x, y, z, el, s=1.0):
    """Shape-coded element crystal (readable without colour): flame, drop, bolt, shards, ring, rock, star, orb."""
    col = ELEM[el]
    glow = mat("el_" + el, col, rough=0.2, emit=col, emit_strength=2.2 if el not in ('light', 'lightning', 'ice') else 1.4)
    dark = mat("el_d_" + el, '#2b2233', rough=0.5)
    if el == 'fire':
        k.cone(x, y, z, 0.13 * s, 0.42 * s, glow, verts=10)
        k.cone(x + 0.08 * s, y, z, 0.06 * s, 0.24 * s, glow, verts=8)
    elif el == 'water':
        o = k.gem(x, y, z + 0.16 * s, 0.13 * s, glow)
        o.scale = (1.0, 1.0, 1.0)
        k.cone(x, y, z + 0.22 * s, 0.11 * s, 0.2 * s, glow, verts=16)
    elif el == 'lightning':
        k.mesh([(x - 0.1 * s, y, z + 0.45 * s), (x + 0.08 * s, y, z + 0.45 * s), (x - 0.01 * s, y, z + 0.24 * s), (x + 0.1 * s, y, z + 0.24 * s),
                (x - 0.06 * s, y, z), (x + 0.0 * s, y, z + 0.2 * s), (x - 0.12 * s, y, z + 0.2 * s)],
               [(0, 1, 2), (2, 3, 4), (2, 4, 6, 5), (0, 2, 5, 6)], glow, "bolt").modifiers.new("solid", 'SOLIDIFY').thickness = 0.06 * s
    elif el == 'ice':
        for dx, h, tilt in ((0, 0.42, 0), (-0.09, 0.28, -18), (0.09, 0.3, 16)):
            o = k.cone(x + dx * s, y, z, 0.06 * s, h * s, glow, verts=4)
            o.rotation_euler = (0, math.radians(tilt), 0)
    elif el == 'wind':
        o = k.torus(x, y, z + 0.22 * s, 0.14 * s, 0.035 * s, glow)
        o.rotation_euler = (math.radians(70), 0, math.radians(20))
        o2 = k.torus(x, y, z + 0.22 * s, 0.09 * s, 0.025 * s, glow)
        o2.rotation_euler = (math.radians(70), 0, math.radians(-30))
    elif el == 'earth':
        o = k.box(x, y, z + 0.04 * s, 0.22 * s, 0.22 * s, 0.22 * s, glow, bevel=0.03)
        o.rotation_euler = (math.radians(20), math.radians(15), math.radians(30))
    elif el == 'light':
        k.gem(x, y, z + 0.2 * s, 0.1 * s, glow)
        for a in range(4):
            o = k.cone(x, y, z + 0.2 * s, 0.03 * s, 0.22 * s, glow, verts=4)
            o.rotation_euler = (0, math.radians(90 * a + 45), 0)
    elif el == 'shadow':
        o = k.gem(x, y, z + 0.2 * s, 0.12 * s, dark)
        o.scale = (1, 1, 1)
        r = k.torus(x, y, z + 0.2 * s, 0.17 * s, 0.022 * s, glow)
        r.rotation_euler = (math.radians(65), 0, 0)
    else:
        k.gem(x, y, z + 0.15 * s, 0.1 * s, glow)


def archer_tower(k, lvl, el='none'):
    plinth(k, 2)
    stone = mat("astone", STONE_C, brick=STONE_B)
    top = mat("atop", TOP_C)
    wood = mat("awood", WOOD_C)
    roof = mat("aroof" + lvl, '#3f8f5a' if lvl != '3a' else '#2f6d85', rough=0.6)
    if lvl == '1':
        for sx in (-1, 1):
            for sy in (-1, 1):
                o = k.cyl(sx * 0.5, sy * 0.45, 0.08, 0.06, 1.45, wood, verts=8)
                o.rotation_euler = (math.radians(-sy * 4), math.radians(sx * 4), 0)
        for sy in (-0.45, 0.45):
            for (a, b2) in ((0.35, 1.05), (1.05, 0.35)):
                o = k.box(0, sy, (a + b2) / 2, 1.0, 0.04, 0.05, wood, bevel=0)
                o.rotation_euler = (0, math.atan2(b2 - a, 1.0), 0)
        k.box(0, 0, 1.45, 1.2, 1.1, 0.12, wood)
        for sx in (-0.55, 0.55):
            k.box(sx, 0, 1.57, 0.06, 1.1, 0.28, wood, bevel=0.01)
        k.box(0, -0.53, 1.57, 1.2, 0.06, 0.28, wood, bevel=0.01)
        for sx in (-0.55, 0.55):
            k.cyl(sx, -0.5, 1.57, 0.035, 0.45, wood, verts=6)
            k.cyl(sx, 0.5, 1.57, 0.035, 0.45, wood, verts=6)
        k.hip(0, 0, 2.02, 1.25, 1.15, 0.85, roof)
        k.flag(0.3, 0.1, 2.3, '#e8b64a', w=0.26, h=0.16, pole=0.4)
    else:
        h = {'2': 1.7, '3a': 2.0, '3b': 1.95}[lvl]
        k.cyl(0, 0.05, 0.08, 0.62, 0.3, stone)
        k.tower(0, 0.05, 0.52, h, stone, top, roof=roof if lvl != '3b' else None, roof_h=0.75)
        k.window(0, 0.05 - 0.5 / DEPTH, h * 0.75, w=0.07, h=0.18)
        k.box(0, 0.05 - 0.52 / DEPTH, 0.08, 0.26, 0.05, 0.38, mat("adoor", '#7a5236'), bevel=0.01)
        if lvl == '3a':
            arm = k.box(0.1, 0.0, h + 0.25, 1.25, 0.08, 0.08, mat("ballista", '#6b4a32'), bevel=0.01)
            arm.rotation_euler = (0, math.radians(-12), math.radians(15))
            k.box(0.1, 0.0, h + 0.1, 0.3, 0.3, 0.16, wood)
        if lvl == '3b':
            for dx in (-0.25, 0, 0.25):
                b2 = k.torus(dx, -0.1, h + 0.3, 0.1, 0.018, mat("bow", '#6b4a32'))
                b2.rotation_euler = (math.radians(90), 0, 0)
                k.box(dx, -0.1, h + 0.2, 0.04, 0.2, 0.04, wood, bevel=0)
        k.flag(0.32, 0.25, h + (0.75 if lvl != '3b' else 0.15), '#e8b64a', w=0.26, h=0.16, pole=0.5)
    if el != 'none':
        element_topper(k, 0, 0.05, (2.55 if lvl == '1' else {'2': 1.7, '3a': 2.0, '3b': 1.95}[lvl] + 0.95), el, 0.8)


def magic_tower(k, lvl, el):
    plinth(k, 2)
    col = ELEM[el]
    wall = mat("mwall", '#bdb3d6', brick='#9a90b8')
    top = mat("mtop", '#d8d0ea')
    roof_hex = {'fire': '#7a3a5a', 'water': '#3f4f9a', 'lightning': '#6a5a3a', 'ice': '#4a6a9a', 'wind': '#3f7a6a',
                'earth': '#6a4a3a', 'light': '#8a7a4a', 'shadow': '#4a2f7a'}[el]
    roof = mat("mroof" + el, roof_hex, rough=0.55)
    band = mat("mband" + el, col, rough=0.3, emit=col, emit_strength=1.6)
    tall = {'1': 1.45, '2': 1.75, 'spire': 2.15, 'catalyst': 1.6}[lvl]
    r = 0.44 if lvl != 'spire' else 0.38
    k.cyl(0, 0.05, 0.08, r + 0.1, 0.25, wall)
    k.cyl(0, 0.05, 0.08, r, tall, wall)
    for i in range(3):
        k.torus(0, 0.05, 0.4 + i * tall * 0.27, r + 0.012, 0.022, band)
    k.box(0, 0.05 - r / DEPTH, 0.08, 0.24, 0.05, 0.36, mat("mdoor", '#5a3f7a'), bevel=0.01)
    k.window(0, 0.05 - r / DEPTH, tall * 0.72, w=0.07, h=0.16, lit=True)
    if lvl == 'catalyst':
        k.cyl(0, 0.05, tall + 0.08, r + 0.12, 0.08, top)
        for a in range(3):
            ang = a * 2.094
            element_topper(k, math.cos(ang) * 0.32, 0.05 + math.sin(ang) * 0.18, tall + 0.2, el, 0.55)
        element_topper(k, 0, 0.05, tall + 0.45, el, 1.0)
        ring = k.torus(0, 0.05, tall + 0.6, 0.42, 0.018, band)
        ring.rotation_euler = (math.radians(70), 0, 0)
    else:
        k.cyl(0, 0.05, tall + 0.08, r + 0.06, 0.06, top)
        k.cone(0, 0.05, tall + 0.14, r + 0.1, 0.7 + (0.2 if lvl == 'spire' else 0), roof)
        element_topper(k, 0, 0.05, tall + 1.05 + (0.2 if lvl == 'spire' else 0), el, 0.9 + (0.15 if lvl != '1' else 0))


def house_bld(k, lvl):
    plinth(k, 2)
    wallc = mat("hwall" + lvl, '#efe2c4' if lvl != '3b' else '#e4dcf2')
    trim = mat("htrim", '#8f6b45')
    roofc = {'1': '#c9a24a', '2': '#c9583f', '3a': '#a63b45', '3b': '#6d58c4'}[lvl]
    roof = mat("hroof" + lvl, roofc, rough=0.7)
    if lvl == '1':
        k.box(0, 0.1, 0.08, 1.15, 0.9, 0.65, wallc)
        k.gable(0, 0.1, 0.73, 1.15, 0.9, 0.58, roof, overhang=0.1)
        k.box(0, 0.1 - 0.46, 0.08, 0.22, 0.04, 0.38, mat("hdoor", '#7a5236'), bevel=0.01)
        for dx in (-0.33, 0.33):
            k.window(dx, 0.1 - 0.46, 0.4, w=0.13, h=0.13, lit=True)
    else:
        k.box(0, 0.12, 0.08, 1.45, 1.0, 0.8, wallc)
        for dx in (-0.72, 0.72):
            k.box(dx, 0.12, 0.08, 0.06, 1.02, 0.8, trim, bevel=0.005)
        k.gable(0, 0.12, 0.88, 1.45, 1.0, 0.62, roof, overhang=0.09)
        k.box(0.42, 0.35, 1.0, 0.16, 0.16, 0.5, mat("chim", '#a5998a'))
        k.box(0, 0.12 - 0.51, 0.08, 0.24, 0.04, 0.42, mat("hdoor", '#7a5236'), bevel=0.01)
        for dx in (-0.45, 0.45):
            k.window(dx, 0.12 - 0.51, 0.45, w=0.15, h=0.15, lit=True)
    if lvl == '3a':
        k.box(-0.6, -0.62, 0.08, 0.45, 0.3, 0.32, mat("stall", '#d9a04a'))
        k.gable(-0.6, -0.62, 0.4, 0.45, 0.3, 0.18, mat("awning", '#e05a4a'), overhang=0.05)
        for i in range(3):
            k.gem(-0.75 + i * 0.15, -0.72, 0.43, 0.04, mat("fruit", '#f2c14e'))
    if lvl == '3b':
        element_topper(k, 0, 0.12, 1.6, 'shadow', 0.7)


def well_bld(k, lvl):
    plinth(k, 2)
    stone = mat("wstone", '#b9b2c9', brick='#948ca8')
    water = mat("water" + lvl, '#5fb8ff', rough=0.1, emit='#7fd8ff' if lvl != '3b' else '#d6a8ff', emit_strength=1.6)
    k.cyl(0, 0, 0.08, 0.62, 0.32, stone, verts=32)
    k.cyl(0, 0, 0.38, 0.5, 0.03, water, verts=32)
    glowc = 'ice' if lvl != '3b' else 'shadow'
    if lvl == '1':
        wood = mat("wwood", WOOD_C)
        for sx in (-0.55, 0.55):
            k.cyl(sx, 0, 0.3, 0.05, 0.85, wood, verts=8)
        k.box(0, 0, 1.0, 1.2, 0.06, 0.06, wood, bevel=0)
        k.gable(0, 0, 1.08, 1.3, 0.7, 0.32, mat("wroof", '#5a7fc0', rough=0.6), overhang=0.05)
    else:
        for dx in (-0.55, 0.55):
            element_topper(k, dx, 0.0, 0.85, glowc, 0.7 if lvl == '2' else 0.85)
        element_topper(k, 0, 0.1, 1.15, glowc, 0.9 if lvl == '2' else 1.1)
    if lvl == '3b':
        rune = mat("rune", '#bfa6ff', emit='#a77bff', emit_strength=3.0)
        for rr in (0.78, 0.9):
            k.torus(0, 0, 0.1, rr, 0.016, rune)


def shrine_bld(k, lvl):
    plinth(k, 2)
    marble = mat("marble", '#f1ebe0', rough=0.5)
    roofc = '#d9b65a' if lvl != '3b' else '#c4553e'
    k.box(0, 0.1, 0.08, 1.4, 1.0, 0.16, marble, bevel=0.03)
    k.box(0, 0.1, 0.24, 1.2, 0.85, 0.08, marble, bevel=0.02)
    for sx in (-0.48, 0.48):
        for sy in (-0.22, 0.42):
            k.cyl(sx, 0.1 + sy, 0.32, 0.07, 0.75, marble, verts=12)
    k.box(0, 0.1, 1.07, 1.3, 0.95, 0.1, marble, bevel=0.02)
    k.gable(0, 0.1, 1.17, 1.3, 0.95, 0.3, mat("sroof" + lvl, roofc, rough=0.4), overhang=0.02)
    if lvl == '3b':
        k.cyl(0, 0.1, 0.32, 0.16, 0.25, mat("brazier", '#8d849e'), verts=10)
        element_topper(k, 0, 0.1, 0.55, 'fire', 1.0)
    else:
        element_topper(k, 0, 0.1, 0.45, 'light', 1.1 + (0.2 if lvl != '1' else 0))


def barracks_bld(k, lvl):
    plinth(k, 2)
    wallc = mat("bwall", '#d9c7a6')
    trim = mat("btrim", '#8f6b45')
    roofc = {'1': '#a8473a', '2': '#93362f', 'legion': '#b8452e', 'elite': '#3f4f8a'}[lvl]
    k.box(-0.1, 0.15, 0.08, 1.5, 0.95, 0.75, wallc)
    for dx in (-0.85, 0.65):
        k.box(dx, 0.15, 0.08, 0.07, 0.97, 0.75, trim, bevel=0.005)
    k.gable(-0.1, 0.15, 0.83, 1.5, 0.95, 0.6, mat("broof" + lvl, roofc, rough=0.6), overhang=0.08)
    k.box(-0.1, 0.15 - 0.49, 0.08, 0.36, 0.04, 0.5, mat("bdoor", '#6b4a32'), bevel=0.01)
    for dx in (-0.6, 0.4):
        k.window(dx, 0.15 - 0.49, 0.45, w=0.14, h=0.14, lit=True)
    wood = mat("bwood", WOOD_C)
    k.cyl(0.78, -0.55, 0.08, 0.035, 0.6, wood, verts=8)
    k.box(0.78, -0.55, 0.5, 0.36, 0.04, 0.04, wood, bevel=0)
    k.gem(0.78, -0.55, 0.75, 0.09, mat("straw", '#d9b27a'))
    k.flag(-0.75, -0.3, 0.83, roofc, w=0.3, h=0.18, pole=0.75)
    if lvl == 'legion':
        for x in (-0.95, 0.75):
            k.flag(x, 0.6, 0.08, '#e8b64a', w=0.34, h=0.22, pole=1.6)
    if lvl == 'elite':
        star = mat("bstar", '#f2d36b', rough=0.3, emit='#f2d36b', emit_strength=0.8)
        element_topper(k, -0.1, 0.15, 1.38, 'light', 0.8)


WALL_STYLE = {
    'wood': dict(h=0.5, t=0.22, col='#a8733f', top='#c08a52', brick=None, post='spike'),
    'stone': dict(h=0.55, t=0.26, col='#cfc5b2', top='#ded6c6', brick='#9c907e', post='crenel'),
    'bastion': dict(h=0.7, t=0.34, col='#b9b0a0', top='#cbc3b4', brick='#7d7365', post='crenel'),
    'rune': dict(h=0.6, t=0.28, col='#b8b0d8', top='#cdc6e6', brick='#7a6fb0', post='rune'),
}


def wall_piece(k, mtl, mask, gate=None):
    """Auto-joining wall (mask 1=N 2=E 4=S 8=W). E/W arms run past the 1-tile frame (cropped by the camera);
    the N arm runs under a holdout box at the tile's back edge, so neighbouring tiles join without outline seams."""
    st = WALL_STYLE[mtl]
    body = mat("w" + mtl, st['col'], brick=st['brick']) if st['brick'] else mat("w" + mtl, st['col'])
    top = mat("wt" + mtl, st['top'])
    h, t = st['h'], st['t']
    k.holdout_box(-3, 3, 0.5, 3.5, -0.2, 4)
    straight = mask in (5, 10) or gate is not None

    def arm(dx, dy):
        L = 1.2
        if dx:
            k.box(dx * L / 2, 0, 0, L, t, h, body, bevel=0.015)
            k.box(dx * L / 2, 0, h, L, t + 0.04, 0.05, top, bevel=0.008)
        else:
            k.box(0, dy * L / 2, 0, t, L, h, body, bevel=0.015)
            k.box(0, dy * L / 2, h, t + 0.04, L, 0.05, top, bevel=0.008)
        if st['post'] == 'spike':
            n = 6
            for i in range(n):
                f = (i + 0.5) / n * L * (dx or dy)
                k.cone(f if dx else 0, f if dy else 0, h, 0.05, 0.14, top, verts=6)
        elif st['post'] in ('crenel', 'rune'):
            n = 4
            for i in range(n):
                f = (i + 0.5) / n * L * (dx or dy)
                k.box(f if dx else 0, f if dy else 0, h + 0.05, 0.1 if dx else t * 0.5, t * 0.5 if dx else 0.1, 0.1, top, bevel=0.008)

    g = gate if gate else None
    if g == 'h' or (g is None and mask & 2):
        arm(1, 0)
    if g == 'h' or (g is None and mask & 8):
        arm(-1, 0)
    if g == 'v' or (g is None and mask & 1):
        arm(0, 1)
    if g == 'v' or (g is None and mask & 4):
        arm(0, -1)
    if g is None and not straight:
        pw = t + 0.12
        k.box(0, 0, 0, pw, pw, h + 0.12, body, bevel=0.02)
        k.box(0, 0, h + 0.12, pw + 0.05, pw + 0.05, 0.05, top, bevel=0.01)
        if st['post'] == 'spike':
            k.cone(0, 0, h + 0.17, pw * 0.55, 0.22, top, verts=4).rotation_euler = (0, 0, math.radians(45))
        else:
            for sx in (-1, 1):
                for sy in (-1, 1):
                    k.box(sx * pw * 0.32, sy * pw * 0.32, h + 0.17, 0.08, 0.08, 0.1, top, bevel=0.008)
    if mtl == 'rune':
        rune = mat("rune", '#bfa6ff', emit='#a77bff', emit_strength=2.4)
        k.box(0, -t / 2 - 0.006, h * 0.45, 0.12, 0.01, 0.12, rune, bevel=0)
    if mtl == 'bastion' and g is None:
        k.box(0, 0, 0, t + 0.2, t + 0.2, 0.12, mat("bfoot", '#8f8676'), bevel=0.02)
    if g:
        door = mat("gdoor" + mtl, '#6b4a32' if mtl != 'rune' else '#5a4a9a')
        pil = t + 0.14
        if g == 'h':
            for sx in (-0.36, 0.36):
                k.box(sx, 0, 0, 0.2, pil, h + 0.3, body, bevel=0.02)
                k.box(sx, 0, h + 0.3, 0.24, pil + 0.04, 0.06, top, bevel=0.01)
            k.box(0, -t / 2 - 0.01, 0, 0.52, 0.04, h + 0.05, door, bevel=0.01)
            k.box(0, 0, h + 0.18, 0.72, t + 0.04, 0.12, top, bevel=0.01)
            for i in range(4):
                k.box(-0.18 + i * 0.12, -t / 2 - 0.035, 0.02, 0.02, 0.01, h, mat("iron", '#4a4652', rough=0.4), bevel=0)
        else:
            for sy in (-0.36, 0.36):
                k.box(0, sy, 0, pil, 0.2, h + 0.3, body, bevel=0.02)
                k.box(0, sy, h + 0.3, pil + 0.04, 0.24, 0.06, top, bevel=0.01)
            k.box(-t / 2 - 0.01, 0, 0, 0.04, 0.52, h + 0.05, door, bevel=0.01)
            k.box(0, 0, h + 0.18, t + 0.04, 0.72, 0.12, top, bevel=0.01)


def ruin_pile(k, size):
    import random as _r
    rng = _r.Random(1000 + size)
    cols = [mat("rub1", '#9c907e'), mat("rub2", '#7a5236'), mat("rub3", '#8d849e')]
    for i in range(7 * size):
        x = rng.uniform(-size * 0.42, size * 0.42)
        y = rng.uniform(-size * 0.42, size * 0.42)
        o = k.box(x, y, 0, rng.uniform(0.1, 0.24), rng.uniform(0.08, 0.2), rng.uniform(0.05, 0.16), rng.choice(cols), bevel=0.02)
        o.rotation_euler = (rng.uniform(-0.4, 0.4), rng.uniform(-0.4, 0.4), rng.uniform(0, 3.1))
    for i in range(size):
        x = rng.uniform(-size * 0.3, size * 0.3)
        o = k.box(x, rng.uniform(-0.2, 0.2) * size, 0, 0.2, 0.22, rng.uniform(0.18, 0.35), cols[0], bevel=0.02)
        o.rotation_euler = (0, rng.uniform(-0.25, 0.25), rng.uniform(0, 1))


def building_spec(key, size, head, build):
    """Canvas: footprint + 0.5 tile side margins, 0.4 tile below the footprint, `head` tiles above its back edge.
    Pivot = footprint centre (towers' range rings, nav footprint and sorting all use the footprint, never the art)."""
    w = int((size + 1.0) * PPU)
    below = 0.4
    h = int(round((below + size + head) * PPU))
    return key, dict(w=w, h=h, px=0.5, py=(below + size / 2) / (below + size + head), build=build, trim=True, catcher=(size + 0.9, size + 0.7), sun_tilt=12)


def wall_spec(key, build):
    # exactly one tile wide so the E/W arms are cut by the frame; 2 tiles tall
    return key, dict(w=64, h=128, px=0.5, py=0.75 / 2.0, build=build, trim=False, catcher=(3.0, 1.6), sun_tilt=12)


for _lvl in ('1', '2', '3a', '3b'):
    _k, _s = building_spec("building/archer_" + _lvl, 2, 2.3 if _lvl != '1' else 2.0, (lambda l: lambda kit: archer_tower(kit, l))(_lvl))
    ASSETS[_k] = _s
    _k, _s = building_spec("building/house_" + _lvl, 2, 1.3, (lambda l: lambda kit: house_bld(kit, l))(_lvl))
    ASSETS[_k] = _s
    _k, _s = building_spec("building/well_" + _lvl, 2, 1.3, (lambda l: lambda kit: well_bld(kit, l))(_lvl))
    ASSETS[_k] = _s
    _k, _s = building_spec("building/shrine_" + _lvl, 2, 1.2, (lambda l: lambda kit: shrine_bld(kit, l))(_lvl))
    ASSETS[_k] = _s
for _lvl in ('1', '2', 'legion', 'elite'):
    _k, _s = building_spec("building/barracks_" + _lvl, 2, 1.4, (lambda l: lambda kit: barracks_bld(kit, l))(_lvl))
    ASSETS[_k] = _s
for _lvl in ('1', '2', 'spire', 'catalyst'):
    for _el in ('fire', 'water', 'lightning', 'ice', 'wind', 'earth', 'light', 'shadow'):
        _k, _s = building_spec(f"building/magic_{_lvl}_{_el}", 2, 2.6, (lambda l, e: lambda kit: magic_tower(kit, l, e))(_lvl, _el))
        ASSETS[_k] = _s
for _m in WALL_STYLE:
    for _mask in range(16):
        _k, _s = wall_spec(f"building/wall_{_m}_{_mask}", (lambda m, mk: lambda kit: wall_piece(kit, m, mk))(_m, _mask))
        ASSETS[_k] = _s
    for _g in ('h', 'v'):
        _k, _s = wall_spec(f"building/gate_{_m}_{_g}", (lambda m, g: lambda kit: wall_piece(kit, m, 0, gate=g))(_m, _g))
        ASSETS[_k] = _s
for _size in (1, 2, 3):
    _k, _s = building_spec(f"building/ruin_{_size}", _size, 0.4, (lambda sz: lambda kit: ruin_pile(kit, sz))(_size))
    ASSETS[_k] = _s



def setup_camera(s, spec):
    W, H = spec['w'] / PPU, spec['h'] / PPU
    cam = bpy.data.cameras.new("cam")
    cam.type = 'ORTHO'
    cam.ortho_scale = max(W, H)
    big = max(W, H)
    cam.shift_x = (0.5 - spec['px']) * W / big
    cam.shift_y = (0.5 - spec['py']) * H / big
    cam.clip_end = 100
    o = bpy.data.objects.new("cam", cam)
    D = 20.0
    o.location = (0, -D * math.cos(EL), D * math.sin(EL))
    o.rotation_euler = (math.pi / 2 - EL, 0, 0)
    s.collection.objects.link(o)
    s.camera = o
    s.render.resolution_x = spec['w'] * SCALE
    s.render.resolution_y = spec['h'] * SCALE
    s.render.resolution_percentage = 100


def main():
    global SCALE, SAMPLES, CATCHER, BUILDING_SUN_TILT
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = "Build/art_staging"
    only = None
    exact = False
    i = 0
    while i < len(argv):
        if argv[i] == "--out":
            out = argv[i + 1]
            i += 2
        elif argv[i] == "--quality":
            if argv[i + 1] == "draft":
                SCALE, SAMPLES = 1, 24
            i += 2
        elif argv[i] == "--list":
            print("ASSET_KEYS " + json.dumps(sorted(ASSETS)), flush=True)
            return
        elif argv[i] == "--exact":
            exact = True
            i += 1
        elif argv[i] == "--only":
            only = []
            i += 1
            while i < len(argv) and not argv[i].startswith("--"):
                only.append(argv[i])
                i += 1
        else:
            i += 1
    raw = os.path.join(out, "_raw")
    os.makedirs(raw, exist_ok=True)
    mpath = os.path.join(raw, "render_manifest.json")
    manifest = json.load(open(mpath)) if os.path.exists(mpath) else {}
    keys = [k for k in ASSETS if not only or any(k == o or (not exact and k.startswith(o)) for o in only)]
    for key in keys:
        spec = ASSETS[key]
        _mats.clear()
        CATCHER = spec.get('catcher', (4.0, 3.9))
        BUILDING_SUN_TILT = spec.get('sun_tilt', 20)
        s, coll, root = reset()
        spec['build'](Kit(coll, root))
        setup_camera(s, spec)
        path = os.path.join(raw, key + ".png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        s.render.filepath = path
        bpy.ops.render.render(write_still=True)
        manifest[key] = dict(w=spec['w'], h=spec['h'], px=round(spec['px'], 6), py=round(spec['py'], 6), ppu=PPU, scale=SCALE,
                             trim=bool(spec.get('trim', False)), src="Tools/ArtPipeline/blender/render_assets.py")
        print("RENDERED", key, flush=True)
    json.dump(manifest, open(mpath, "w"), indent=1, sort_keys=True)
    print(f"RENDER_DONE {len(keys)} assets", flush=True)


if __name__ == "__main__":
    main()
