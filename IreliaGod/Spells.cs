using LeagueSharp;
using LeagueSharp.Common;

namespace IreliaGod
{
    class Spells
    {
        public static Spell Q { get; private set; }
        public static Spell W { get; private set; }
        public static Spell E { get; private set; }
        public static Spell R { get; private set; }
        public static SpellSlot Ignite { get; private set; }

        public static void Initialize()
        {
            Q = new Spell(SpellSlot.Q, 650);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R, 1000);

            Q.SetTargetted(0f, 2200);
            R.SetSkillshot(0.5f, 120, 1600, false, SkillshotType.SkillshotLine);


            Ignite = ObjectManager.Player.GetSpellSlot("summonerdot");
        }
    }
}
