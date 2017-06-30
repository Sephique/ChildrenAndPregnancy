using System;
using RimWorld;
using Verse;

namespace RimWorldChildren
{
	public class ThoughtWorker_ScaredOfTheDark : ThoughtWorker_Dark
	{
		//
		// Methods
		//
		protected override ThoughtState CurrentStateInternal (Pawn p)
		{
			// Make sure it only gets applied to kids
			if (p.ageTracker.CurLifeStageIndex < 1 || p.ageTracker.CurLifeStageIndex > 2)
				return false;
			// Psychopath kids doesn't afraid of anything
			if (p.story.traits.HasTrait (TraitDefOf.Psychopath))
				return false;
			return p.Awake () && p.needs.mood.recentMemory.TicksSinceLastLight > 800;
		}
	}
}