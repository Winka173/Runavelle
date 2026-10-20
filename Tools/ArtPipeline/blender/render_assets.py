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
SCALE = 2  # render at 2x, downsampled by artpipe


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
    s.cycles.samples = 96
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
    bg.inputs[1].default_value = 0.55
    s.world = w
    # key (upper-left, front), warm rim (back-right)
    key = bpy.data.lights.new("key", 'SUN')
    key.energy = 3.4
    key.angle = math.radians(6)
    key.color = srgb('#fff1dc')[:3]
    ko = bpy.data.objects.new("key", key)
    ko.rotation_euler = (math.radians(20), 0, math.radians(-35))
    s.collection.objects.link(ko)
    rim = bpy.data.lights.new("rim", 'SUN')
    rim.energy = 1.2
    rim.color = srgb('#ffd2a0')[:3]
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
    sc.scale = (4.0, 3.9 * DEPTH, 1)
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
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = "Build/art_staging"
    only = None
    i = 0
    while i < len(argv):
        if argv[i] == "--out":
            out = argv[i + 1]
            i += 2
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
    keys = [k for k in ASSETS if not only or any(k == o or k.startswith(o) for o in only)]
    for key in keys:
        spec = ASSETS[key]
        _mats.clear()
        s, coll, root = reset()
        spec['build'](Kit(coll, root))
        setup_camera(s, spec)
        path = os.path.join(raw, key + ".png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        s.render.filepath = path
        bpy.ops.render.render(write_still=True)
        manifest[key] = dict(w=spec['w'], h=spec['h'], px=round(spec['px'], 4), py=round(spec['py'], 4), ppu=PPU, scale=SCALE,
                             src="Tools/ArtPipeline/blender/render_assets.py")
        print("RENDERED", key, flush=True)
    json.dump(manifest, open(mpath, "w"), indent=1, sort_keys=True)
    print(f"RENDER_DONE {len(keys)} assets", flush=True)


main()
