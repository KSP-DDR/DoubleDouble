using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Experience;

namespace TerrasIgnotas
{
    public class TIInterstellarSkill : ExperienceEffect
    {
        static string[] skills = new string[]
        {
            " can increase hyperdrive accuracy.",
            " can decrease antenna power requirements.",
            " can increase science gains."
        };

        protected override float GetDefaultValue()
        {
            return 0;
        }

        protected override string GetDescription()
        {
            ProtoCrewMember crew = Parent.CrewMember;
            string pronoun;
            if (crew.gender == ProtoCrewMember.Gender.Female)
            {
                pronoun = "She";
            }
            else
            {
                pronoun = "He";
            }
            int xp = Parent.CrewMemberExperienceLevel(6);
            return pronoun + skills[xp];
        }

        
        public TIInterstellarSkill(ExperienceTrait parent, float[] modifiers) : base (parent)
        {

        }

        public TIInterstellarSkill(ExperienceTrait parent) : base(parent)
        {

        }
    }
}
