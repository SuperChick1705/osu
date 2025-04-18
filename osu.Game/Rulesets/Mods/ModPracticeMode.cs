using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Configuration;
using osu.Game.Screens.Play;
using System.Linq;
using osu.Framework.Bindables;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class ModPracticeMode : Mod, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToPlayer
    {
        // ... (existing properties and bindables)

        private bool isInBlankPeriod = false; // Track if we're in the 1.5s blank phase

        private void OnJudgement(JudgementResult judgement)
        {
            if (isInBlankPeriod) return; // Ignore misses during blank period

            if (FcMode.Value && judgement.IsMiss)
                RestartToNearestCheckpoint();
        }

        private void RestartToNearestCheckpoint()
        {
            double currentTime = player.GameplayClockContainer.CurrentTime;
            double nearestCheckpoint = 0;

            foreach (var checkpoint in checkpoints)
            {
                if (checkpoint <= currentTime)
                    nearestCheckpoint = checkpoint;
                else
                    break;
            }

            // Hide objects for 1.5s after checkpoint
            isInBlankPeriod = true;
            Schedule(() =>
            {
                foreach (var obj in drawableRuleset.Objects)
                {
                    if (obj.StartTime >= nearestCheckpoint && obj.StartTime < nearestCheckpoint + 1500)
                        obj.Alpha = 0;
                }
            });

            // Seek to checkpoint (with 3s countdown)
            player.GameplayClockContainer.Seek(nearestCheckpoint - 3000);

            // Re-enable checkpoints after 1.5s
            Schedule(() =>
            {
                isInBlankPeriod = false;
                foreach (var obj in drawableRuleset.Objects)
                    obj.Alpha = 1; // Restore visibility
            }, 1500);
        }
    }
}
