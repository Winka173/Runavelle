using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Particle silhouette used by an element's impact (shape + motion, so elements read without colour).</summary>
    public enum FxShape { Ember, Droplet, Arc, Shard, Swirl, Fragment, Ray, Wisp }
    public enum FxMotion { Rise, Splash, Zigzag, Burst, Spiral, Fall, Radial, Sink }

    public struct ElementFx
    {
        public FxShape Shape;
        public FxMotion Motion;
        /// <summary>Colour-blind status icon (distinct shape per element; drawn as a sprite, never as a font glyph).</summary>
        public string Icon;
        public string Sound;
    }

    /// <summary>How a reaction is showcased (scaled by its gameplay importance).</summary>
    public enum ReactionShowcase { Steam, Blast, MeltPool, Vortex, Eruption, ChainArcs, ShardCage, Downpour, Quagmire, Shatter, StormCell, Pull, Whiteout, Spikes, Dust, Eclipse, Bulwark, HauntArcs, Flare, SoulChill, Cleanse, Resonance }

    public struct ReactionFx
    {
        public ReactionShowcase Showcase;
        /// <summary>1 = ordinary, 2 = major (control/AoE finishers) — scales size, shake and sound.</summary>
        public int Weight;
    }

    public static class ElementGrammar
    {
        public static readonly Dictionary<ElementId, ElementFx> Elements = new Dictionary<ElementId, ElementFx>
        {
            [ElementId.Fire] = new ElementFx { Shape = FxShape.Ember, Motion = FxMotion.Rise, Icon = "icon/el_fire", Sound = "fx_fire" },
            [ElementId.Water] = new ElementFx { Shape = FxShape.Droplet, Motion = FxMotion.Splash, Icon = "icon/el_water", Sound = "fx_water" },
            [ElementId.Lightning] = new ElementFx { Shape = FxShape.Arc, Motion = FxMotion.Zigzag, Icon = "icon/el_lightning", Sound = "zap" },
            [ElementId.Ice] = new ElementFx { Shape = FxShape.Shard, Motion = FxMotion.Burst, Icon = "icon/el_ice", Sound = "fx_ice" },
            [ElementId.Wind] = new ElementFx { Shape = FxShape.Swirl, Motion = FxMotion.Spiral, Icon = "icon/el_wind", Sound = "fx_wind" },
            [ElementId.Earth] = new ElementFx { Shape = FxShape.Fragment, Motion = FxMotion.Fall, Icon = "icon/el_earth", Sound = "fx_earth" },
            [ElementId.Light] = new ElementFx { Shape = FxShape.Ray, Motion = FxMotion.Radial, Icon = "icon/el_light", Sound = "fx_light" },
            [ElementId.Shadow] = new ElementFx { Shape = FxShape.Wisp, Motion = FxMotion.Sink, Icon = "icon/el_shadow", Sound = "fx_shadow" },
        };

        public static ElementFx For(ElementId e) => Elements.TryGetValue(e, out var f) ? f : new ElementFx { Shape = FxShape.Fragment, Motion = FxMotion.Burst, Icon = "icon/el_none", Sound = "hit" };

        public static ReactionFx For(ReactionId r)
        {
            switch (r)
            {
                case ReactionId.SteamBurst: return new ReactionFx { Showcase = ReactionShowcase.Steam, Weight = 1 };
                case ReactionId.Overload: return new ReactionFx { Showcase = ReactionShowcase.Blast, Weight = 2 };
                case ReactionId.Melt: return new ReactionFx { Showcase = ReactionShowcase.MeltPool, Weight = 2 };
                case ReactionId.Firestorm: return new ReactionFx { Showcase = ReactionShowcase.Vortex, Weight = 2 };
                case ReactionId.Magma: return new ReactionFx { Showcase = ReactionShowcase.Eruption, Weight = 2 };
                case ReactionId.Electrocharge: return new ReactionFx { Showcase = ReactionShowcase.ChainArcs, Weight = 1 };
                case ReactionId.FlashFreeze: return new ReactionFx { Showcase = ReactionShowcase.ShardCage, Weight = 2 };
                case ReactionId.Rainfall: return new ReactionFx { Showcase = ReactionShowcase.Downpour, Weight = 1 };
                case ReactionId.Mire: return new ReactionFx { Showcase = ReactionShowcase.Quagmire, Weight = 1 };
                case ReactionId.Superconduct: return new ReactionFx { Showcase = ReactionShowcase.Shatter, Weight = 1 };
                case ReactionId.StaticStorm: return new ReactionFx { Showcase = ReactionShowcase.StormCell, Weight = 2 };
                case ReactionId.Magnetize: return new ReactionFx { Showcase = ReactionShowcase.Pull, Weight = 1 };
                case ReactionId.Blizzard: return new ReactionFx { Showcase = ReactionShowcase.Whiteout, Weight = 2 };
                case ReactionId.IceSpikes: return new ReactionFx { Showcase = ReactionShowcase.Spikes, Weight = 1 };
                case ReactionId.Sandstorm: return new ReactionFx { Showcase = ReactionShowcase.Dust, Weight = 1 };
                case ReactionId.Eclipse: return new ReactionFx { Showcase = ReactionShowcase.Eclipse, Weight = 2 };
                case ReactionId.Fortify: return new ReactionFx { Showcase = ReactionShowcase.Bulwark, Weight = 1 };
                case ReactionId.HauntedArc: return new ReactionFx { Showcase = ReactionShowcase.HauntArcs, Weight = 1 };
                case ReactionId.SolarFlare: return new ReactionFx { Showcase = ReactionShowcase.Flare, Weight = 2 };
                case ReactionId.SoulFrost: return new ReactionFx { Showcase = ReactionShowcase.SoulChill, Weight = 1 };
                case ReactionId.Purify: return new ReactionFx { Showcase = ReactionShowcase.Cleanse, Weight = 1 };
                default: return new ReactionFx { Showcase = ReactionShowcase.Resonance, Weight = 1 };
            }
        }
    }
}
