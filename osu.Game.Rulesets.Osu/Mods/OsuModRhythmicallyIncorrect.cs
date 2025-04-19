using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModRhythmicallyIncorrect : Mod, IApplicableToScoreProcessor
    {
        public override string Name => "Rhythmically Incorrect";
        public override string Acronym => "RI";
        public override ModType Type => ModType.Fun;
        public override string Description => "Your hits are technically correct... the best kind of correct.";
        public override double ScoreMultiplier => 1;

        public void ApplyToScoreProcessor(ScoreProcessor scoreProcessor)
        {
            scoreProcessor.NewJudgement += judgement =>
            {
                if (judgement.Judgement is OsuHitWindows.OsuJudgement osuJudgement)
                {
                    switch (judgement.HitResult)
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
    }
}
