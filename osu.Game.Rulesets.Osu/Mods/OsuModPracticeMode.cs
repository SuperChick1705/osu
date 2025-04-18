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
        private DrawableRuleset<OsuHitObject>? drawableRuleset;
        private bool isInBlankPeriod;
        private ScheduledDelegate? blankPeriodEndSchedule;
        private IBeatmap? workingBeatmap;
        private IScoreProcessor? scoreProcessor;
        private IGameplayClock? gameplayClock;

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            this.drawableRuleset = drawableRuleset;
            this.workingBeatmap = drawableRuleset.Beatmap;
            
            checkpoints = workingBeatmap.HitObjects
                .Where(o => o.StartTime % (CheckpointInterval.Value * 1000) < 50)
                .Select(o => o.StartTime)
                .ToArray();
        }

        public void ApplyToPlayer(Player player)
        {
            // Get dependencies through DI or reflection
            scoreProcessor = player.Dependencies.Get<IScoreProcessor>();
            gameplayClock = player.Dependencies.Get<IGameplayClock>();
            
            if (workingBeatmap?.Track != null)
            {
                workingBeatmap.Track.Seek(StartTime.Value * 1000 - 3000);
            }
            
            scoreProcessor.NewJudgement += OnJudgement;
        }

        private void OnJudgement(JudgementResult judgement)
        {
            if (FcMode.Value && judgement.IsMiss && !isInBlankPeriod)
                RequestRestart();
        }

        private void RequestRestart()
        {
            if (drawableRuleset == null || gameplayClock == null) return;

            blankPeriodEndSchedule?.Cancel();

            double currentTime = gameplayClock.CurrentTime;
            double nearestCheckpoint = checkpoints.LastOrDefault(c => c <= currentTime);

            isInBlankPeriod = true;
            foreach (var obj in drawableRuleset.Objects.OfType<DrawableHitObject>())
            {
                if (obj.HitObject.StartTime >= nearestCheckpoint && obj.HitObject.StartTime < nearestCheckpoint + 1500)
                    obj.Alpha = 0;
            }

            gameplayClock.Seek(nearestCheckpoint - 3000);

            blankPeriodEndSchedule = (drawableRuleset as DrawableRuleset)?.Schedule(() =>
            {
                isInBlankPeriod = false;
                foreach (var obj in drawableRuleset.Objects.OfType<DrawableHitObject>())
                    obj.Alpha = 1;
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            blankPeriodEndSchedule?.Cancel();
            if (scoreProcessor != null)
            {
                scoreProcessor.NewJudgement -= OnJudgement;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
