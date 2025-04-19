using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Configuration;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModAdjustAR : Mod, IApplicableToDifficulty, IReadFromConfig
    {
        public override string Name => "Adjust AR";
        public override string Acronym => "AR";
        public override ModType Type => ModType.DifficultyReduction;
        public override double ScoreMultiplier => 0.5;
        
        [SettingSource("AR Value", "Set custom approach rate")]
        public BindableFloat CustomAR { get; } = new BindableFloat
        {
            MinValue = -10,
            MaxValue = 11,
            Default = 5, // Default to normal AR5
            Precision = 0.1f
        };

        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            // Completely override the original AR
            difficulty.ApproachRate = CustomAR.Value;
        }
    }
}
