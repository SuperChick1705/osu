using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Configuration;
using osu.Game.Screens.Play;
using osu.Game.Rulesets.UI;
using System.Linq;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class ModPracticeMode : Mod, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToPlayer
    {
        public override string Name => "Practice Mode";
        public override string Acronym => "PM";
        public override string Description => "Start at a custom time with checkpoints.";
        public override ModType Type => ModType.Training;

        [SettingSource("Start Time (s)", "Where to start the beatmap (in seconds)")]
        public BindableFloat StartTime { get; } = new BindableFloat
        {
            MinValue = 0,
            MaxValue = 1000,
            Default = 0,
            Precision = 0.1f
        };

        [SettingSource("Checkpoint Interval (s)", "How often checkpoints are placed (in seconds)")]
        public BindableFloat CheckpointInterval { get; } = new BindableFloat
        {
            MinValue = 1,
            MaxValue = 30,
            Default = 5,
            Precision = 0.1f
        };

        [SettingSource("FC Mode", "Restart on misses if enabled")]
        public BindableBool FcMode { get; } = new BindableBool();

        private double[] checkpoints;
        private Player player;
        private DrawableRuleset<OsuHitObject> drawableRuleset;

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            this.drawableRuleset = drawableRuleset;
            var beatmap = drawableRuleset.Beatmap;

            // Generate checkpoints at the specified interval
            double lastCheckpointTime = 0;
            checkpoints = beatmap.HitObjects
                .Where(obj => obj.StartTime >= lastCheckpointTime + CheckpointInterval.Value * 1000)
                .Select(obj => 
                {
                    lastCheckpointTime = obj.StartTime;
                    return lastCheckpointTime;
                })
                .ToArray();

            // Seek to start time (with 3s countdown)
            beatmap.Track.Seek(StartTime.Value * 1000 - 3000);
        }

        public void ApplyToPlayer(Player player)
        {
            this.player = player;
            player.OnFail += OnFail;
            player.ScoreProcessor.NewJudgement += OnJudgement;
        }

        private void OnFail()
        {
            if (!FcMode.Value)
                RestartToNearestCheckpoint();
        }

        private void OnJudgement(JudgementResult judgement)
        {
            if (FcMode.Value && judgement.IsMiss)
                RestartToNearestCheckpoint();
        }

        private void RestartToNearestCheckpoint()
        {
            double currentTime = player.GameplayClockContainer.CurrentTime;
            double nearestCheckpoint = 0;

            // Find the nearest checkpoint before the failure time
            foreach (var checkpoint in checkpoints)
            {
                if (checkpoint <= currentTime)
                    nearestCheckpoint = checkpoint;
                else
                    break;
            }

            // Hide all hit objects for 1.5s after the checkpoint
            double blankStartTime = nearestCheckpoint;
            double blankEndTime = nearestCheckpoint + 1500; // 1.5s blank space

            foreach (var obj in drawableRuleset.Objects)
            {
                if (obj.StartTime >= blankStartTime && obj.StartTime < blankEndTime)
                    obj.Alpha = 0; // Hide the object
                else if (obj.StartTime >= blankEndTime)
                    obj.Alpha = 1; // Show objects after the blank period
            }

            // Seek to checkpoint (with 3s countdown)
            player.GameplayClockContainer.Seek(nearestCheckpoint - 3000);
        }
    }
}
