using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osu.Game.Utils;
using System.Linq;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModPracticeMode : Mod, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToPlayer, IDisposable
    {
        public override string Name => "Practice Mode";
        public override string Acronym => "PM";
        public override LocalisableString Description => "Start at a custom time with checkpoints at set intervals.";
        public override ModType Type => ModType.Training;
        public override double ScoreMultiplier => 1.0;

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

        private double[] checkpoints = Array.Empty<double>();
        private Player? player;
        private DrawableRuleset<OsuHitObject>? drawableRuleset;
        private bool isInBlankPeriod;
        private ScheduledDelegate? blankPeriodEndSchedule;

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            this.drawableRuleset = drawableRuleset;
            var workingBeatmap = (player?.GameplayState?.Beatmap ?? drawableRuleset.Beatmap);
            
            checkpoints = workingBeatmap.HitObjects
                .Where(o => o.StartTime % (CheckpointInterval.Value * 1000) < 50)
                .Select(o => o.StartTime)
                .ToArray();

            workingBeatmap.Track.Seek(StartTime.Value * 1000 - 3000);
        }

        public void ApplyToPlayer(Player player)
        {
            this.player = player;
            player.Fail += OnFail;
            player.ScoreProcessor.NewJudgement += OnJudgement;
        }

        private void OnFail()
        {
            if (!FcMode.Value && !isInBlankPeriod && player != null && drawableRuleset != null)
                RestartToNearestCheckpoint();
        }

        private void OnJudgement(JudgementResult judgement)
        {
            if (FcMode.Value && judgement.IsMiss && !isInBlankPeriod && player != null && drawableRuleset != null)
                RestartToNearestCheckpoint();
        }

        private void RestartToNearestCheckpoint()
        {
            if (player == null || drawableRuleset == null) return;

            blankPeriodEndSchedule?.Cancel();

            double currentTime = player.GameplayClockContainer.CurrentTime;
            double nearestCheckpoint = checkpoints.LastOrDefault(c => c <= currentTime);

            isInBlankPeriod = true;
            foreach (var obj in drawableRuleset.Objects.OfType<DrawableHitObject>())
            {
                if (obj.HitObject.StartTime >= nearestCheckpoint && obj.HitObject.StartTime < nearestCheckpoint + 1500)
                    obj.Alpha = 0;
            }

            player.GameplayClockContainer.Seek(nearestCheckpoint - 3000);

            blankPeriodEndSchedule = player.Schedule(() =>
            {
                isInBlankPeriod = false;
                foreach (var obj in drawableRuleset.Objects.OfType<DrawableHitObject>())
                    obj.Alpha = 1;
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            blankPeriodEndSchedule?.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
