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

	public class ThoughtWorker_NearParents : ThoughtWorker
	{
		const int maxDist = 8;
		//
		// Methods
		//
		protected override ThoughtState CurrentStateInternal (Pawn p)
		{
			if (p.ageTracker.CurLifeStageIndex > AgeStage.Toddler)
				return false;
			Pawn mother = p.relations.GetFirstDirectRelationPawn (PawnRelationDefOf.Parent, x => x.gender == Gender.Female);
			Pawn father = p.relations.GetFirstDirectRelationPawn (PawnRelationDefOf.Parent, x => x.gender == Gender.Male);
			byte parents = 0;
			if (mother != null && mother.GetRoom () == p.GetRoom () && mother.Position.DistanceTo (p.Position) < maxDist)
				parents += 1;
			if (father != null && father.GetRoom () == p.GetRoom () && father.Position.DistanceTo (p.Position) < maxDist)
				parents += 2;
			ThoughtState[] states = {false, ThoughtState.ActiveAtStage (0), ThoughtState.ActiveAtStage (1), ThoughtState.ActiveAtStage (2)};
			return states [parents];
		}
	}
}