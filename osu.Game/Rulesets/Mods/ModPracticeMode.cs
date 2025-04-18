using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Configuration;
using osu.Game.Screens.Play;
using osu.Framework.Bindables;
using osu.Framework.Threading;
using System.Linq;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class ModPracticeMode : Mod, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToPlayer
    {
        public override string Name => "Practice Mode";
        public override string Acronym => "PM";
        public override string Description => "Start at a custom time with checkpoints at set intervals.";
        public override ModType Type => ModType.Training;

        [SettingSource("Start Time (s)", "Where to start the beatmap")]
        public BindableFloat StartTime { get; } = new BindableFloat
        {
            MinValue = 0,
            MaxValue = 1000,
            Default = 0,
            Precision = 0.1f
        };

        [SettingSource("Checkpoint Interval (s)", "Distance between checkpoints")]
        public BindableFloat CheckpointInterval { get; } = new BindableFloat
        {
            MinValue = 1,
            MaxValue = 30,
            Default = 5,
            Precision = 0.1f
        };

        [SettingSource("FC Mode", "Restart on misses")]
        public BindableBool FcMode { get; } = new BindableBool();

        private double[] checkpoints;
        private Player player;
        private DrawableRuleset<OsuHitObject> drawableRuleset;
        private bool isInBlankPeriod;
        private ScheduledDelegate blankPeriodEndSchedule;

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            this.drawableRuleset = drawableRuleset;
            var beatmap = drawableRuleset.Beatmap;

            // Generate checkpoints at intervals
            checkpoints = beatmap.HitObjects
                .Where(o => o.StartTime % (CheckpointInterval.Value * 1000) < 50) // ~every X seconds
                .Select(o => o.StartTime)
                .ToArray();

            beatmap.Track.Seek(StartTime.Value * 1000 - 3000); // Start with 3s countdown
        }

        public void ApplyToPlayer(Player player)
        {
            this.player = player;
            player.OnFail += OnFail;
            player.ScoreProcessor.NewJudgement += OnJudgement;
        }

        private void OnFail()
        {
            if (!FcMode.Value && !isInBlankPeriod)
                RestartToNearestCheckpoint();
        }

        private void OnJudgement(JudgementResult judgement)
        {
            if (FcMode.Value && judgement.IsMiss && !isInBlankPeriod)
                RestartToNearestCheckpoint();
        }

        private void RestartToNearestCheckpoint()
        {
            blankPeriodEndSchedule?.Cancel(); // Cancel any pending blank period end

            double currentTime = player.GameplayClockContainer.CurrentTime;
            double nearestCheckpoint = checkpoints.LastOrDefault(c => c <= currentTime);

            // Activate blank period
            isInBlankPeriod = true;
            foreach (var obj in drawableRuleset.Objects)
            {
                if (obj.StartTime >= nearestCheckpoint && obj.StartTime < nearestCheckpoint + 1500)
                    obj.Alpha = 0;
            }

            // Seek to checkpoint (with 3s countdown)
            player.GameplayClockContainer.Seek(nearestCheckpoint - 3000);

            // Schedule end of blank period
            blankPeriodEndSchedule = player.Schedule(() =>
            {
                isInBlankPeriod = false;
                foreach (var obj in drawableRuleset.Objects)
                    obj.Alpha = 1;
            }, 1500);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            blankPeriodEndSchedule?.Cancel();
        }
    }
}
