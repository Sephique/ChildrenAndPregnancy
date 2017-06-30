using System;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace RimWorldChildren
{
	public class ThoughtWorker_CryingBaby : ThoughtWorker
	{
		//
		// Methods
		//
		protected override ThoughtState CurrentStateInternal (Pawn p)
		{
			// Does not affect babies and toddlers
			if (p.ageTracker.CurLifeStageIndex < 2 || p.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) <= 0.1f)
				return ThoughtState.Inactive;

			// Find all crying babies in the vicinity
			List<Pawn> cryingBabies = new List<Pawn>();
			foreach (Pawn mapPawn in p.MapHeld.mapPawns.AllPawnsSpawned) {
				if (mapPawn.RaceProps.Humanlike &&
				   mapPawn.ageTracker.CurLifeStageIndex == 0 &&
				   mapPawn.health.hediffSet.HasHediff (HediffDef.Named ("UnhappyBaby")) &&
					mapPawn.PositionHeld.InHorDistOf(p.PositionHeld, 24) &&
					mapPawn.PositionHeld.GetRoomOrAdjacent(mapPawn.MapHeld).ContainedAndAdjacentThings.Contains(p)){
					cryingBabies.Add (mapPawn);
				}
			}
			if (cryingBabies.Count > 0) {
				if (cryingBabies.Count == 1)
					return ThoughtState.ActiveAtStage (0);
				else if (cryingBabies.Count <= 3)
					return ThoughtState.ActiveAtStage (1);
				else if (cryingBabies.Count >= 4)
					return ThoughtState.ActiveAtStage (2);
			}
			return ThoughtState.Inactive;
		}
	}
}

