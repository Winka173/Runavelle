using System;

namespace Runavelle.Core
{
    public enum MusicState { Title, Day, Dusk, Fight, Danger, Boss, Victory, Defeat }

    /// <summary>
    /// Chooses the music layer from combat intensity with hysteresis and a minimum dwell time, so the track never
    /// thrashes frame to frame. Inputs are read from the simulation; output is only a track name.
    /// </summary>
    public sealed class MusicDirector
    {
        public const float MinDwell = 6f;
        public const float DangerEnter = 0.62f, DangerExit = 0.4f;

        public MusicState State { get; private set; } = MusicState.Title;
        private float _dwell;
        private float _danger;         // smoothed 0..1
        private float _breachPulse;    // decays after a wall breach
        private float _relief;         // after a boss defeat: calm Fight layer briefly

        public static string Track(MusicState s)
        {
            switch (s)
            {
                case MusicState.Day: return "music_day";
                case MusicState.Dusk: return "music_dusk";
                case MusicState.Fight: return "music_night";
                case MusicState.Danger: return "music_danger";
                case MusicState.Boss: return "music_boss";
                case MusicState.Victory: return "music_day";
                case MusicState.Defeat: return "music_title";
                default: return "music_title";
            }
        }

        public void Breach() => _breachPulse = 1f;
        public void Relief() { _relief = 8f; _danger = 0f; }

        /// <summary>Danger signal: castle HP lost, enemies near the castle, recent breaches (0..1).</summary>
        public static float DangerSignal(float castleHpFrac, int enemiesNearCastle, float breachPulse)
            => Math.Min(1f, (1f - castleHpFrac) * 1.2f + Math.Min(1f, enemiesNearCastle / 10f) * 0.5f + breachPulse * 0.45f);

        public MusicState Update(float dt, RunPhase phase, bool dusk, bool bossAlive, float castleHpFrac, int enemiesNearCastle)
        {
            _dwell += dt;
            _breachPulse = Math.Max(0f, _breachPulse - dt / 10f);
            _relief = Math.Max(0f, _relief - dt);
            float target = DangerSignal(castleHpFrac, enemiesNearCastle, _breachPulse);
            _danger += (target - _danger) * Math.Min(1f, dt * 1.5f);

            MusicState want;
            switch (phase)
            {
                case RunPhase.Night:
                    if (_relief > 0f) want = MusicState.Fight;
                    else if (State == MusicState.Danger) want = _danger > DangerExit ? MusicState.Danger : (bossAlive ? MusicState.Boss : MusicState.Fight);
                    else want = _danger >= DangerEnter ? MusicState.Danger : (bossAlive ? MusicState.Boss : MusicState.Fight);
                    break;
                case RunPhase.Day:
                    want = dusk ? MusicState.Dusk : MusicState.Day;
                    break;
                case RunPhase.Victory: want = MusicState.Victory; break;
                case RunPhase.Defeat: want = MusicState.Defeat; break;
                default: want = MusicState.Day; break; // report / drafts: calm
            }
            // phase changes switch immediately; intensity changes inside a phase respect the dwell time
            bool calmNow = State == MusicState.Day || State == MusicState.Title || State == MusicState.Victory || State == MusicState.Defeat;
            bool calmWant = want == MusicState.Day || want == MusicState.Victory || want == MusicState.Defeat;
            bool phaseJump = State == MusicState.Title || calmNow != calmWant || want == MusicState.Dusk || State == MusicState.Dusk || want == MusicState.Victory || want == MusicState.Defeat;
            if (want != State && (phaseJump || _dwell >= MinDwell))
            {
                State = want;
                _dwell = 0f;
            }
            return State;
        }

        public float Danger => _danger;
    }
}
