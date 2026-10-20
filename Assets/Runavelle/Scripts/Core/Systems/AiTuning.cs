namespace Runavelle.Core
{
    /// <summary>
    /// Tunable utility weights for autonomous soldiers (one place to balance army behaviour; documented in
    /// Docs/AIBehaviorAndPriorities.md). Values are deterministic constants — no runtime randomness.
    /// </summary>
    public static class AiTuning
    {
        // --- target scoring (DefenderAI.ChooseThreat)
        public static float WeightProximity = 3f;          // 3 / (1 + distance to soldier)
        public static float WeightCastleThreat = 2f;       // (1 - flowDistance/60) * w
        public static float BonusHittingStructure = 1.2f;  // enemy currently attacking one of our structures
        public static float BonusHittingUnit = 0.6f;
        public static float BonusSiege = 1.0f;             // rams / gate breakers
        public static float BonusAntiAirMatch = 1.5f;      // flying soldier vs flyer
        public static float BonusSpearVsLarge = 0.8f;
        public static float BonusInsideBreach = 0.8f;      // enemy standing in a wall gap opened this night
        public static float Stickiness = 0.5f;             // keep current target (no flapping)
        public static float LeashSlack = 1.5f;             // never pick targets further than leash + slack from home

        // --- ranged spacing
        public static float RangedMinRange = 2.5f;         // soldiers with Range above this count as ranged
        public static float KiteTrigger = 1.15f;           // an enemy this close → step back toward home
        public static float KiteStep = 1.4f;

        // --- pursuit cap (fast units)
        public static float FastSpeed = 3.2f;              // cavalry / griffins
        public static float MaxChaseSeconds = 3.5f;        // give up a target not reached in this time, return to post
        public static float ChaseCooldown = 2f;            // ignore that target for this long afterwards
    }
}
