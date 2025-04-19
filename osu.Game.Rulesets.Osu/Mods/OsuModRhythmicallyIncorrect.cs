using osu.Framework.Localisation;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Osu.Judgements;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModRhythmicallyIncorrect : Mod, IApplicableToScoreProcessor
    {
        public override string Name => "Rhythmically Incorrect";
        public override string Acronym => "RI";
        public override ModType Type => ModType.Fun;
        public override LocalisableString Description => "Your hits are technically correct... the best kind of correct.";
        public override double ScoreMultiplier => 1;

        public void ApplyToScoreProcessor(ScoreProcessor scoreProcessor)
        {
            scoreProcessor.NewJudgement += judgement =>
            {
                if (judgement.Judgement is OsuJudgement && judgement.Type != HitResult.Miss)
                {
                    switch (judgement.Type)
                    {
                        case HitResult.Great: // 300
                            judgement.Type = HitResult.Meh; // 50
                            break;

                        case HitResult.Meh: // 50
                            judgement.Type = HitResult.Great; // 300
                            break;
                    }
                }
            };
        }

        public ScoreRank AdjustRank(ScoreRank rank, double accuracy) => rank;
    }
}
