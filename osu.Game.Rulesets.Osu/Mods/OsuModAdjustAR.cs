using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;
using System;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModAdjustAR : Mod, IApplicableToDifficulty
    {
        public override string Name => "Adjust AR";
        public override string Acronym => "AR";
        public override LocalisableString Description => "Adjust approach rate";
        public override ModType Type => ModType.DifficultyReduction;
        public override double ScoreMultiplier => 0.5;

        [SettingSource("AR Value", "Set custom approach rate")]
        public BindableFloat CustomAR { get; } = new BindableFloat
        {
            MinValue = -10,
            MaxValue = 11,
            Default = 5,
            Precision = 0.1f
        };

        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            // Directly override the AR value
            difficulty.ApproachRate = CustomAR.Value;
        }
    }
}
