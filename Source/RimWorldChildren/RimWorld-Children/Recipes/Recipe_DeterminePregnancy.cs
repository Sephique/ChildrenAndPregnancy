using RimWorld;
using System;
using Verse;
using System.Collections.Generic;
using System.Diagnostics;

namespace RimWorldChildren
{
	public class Recipe_DeterminePregnancy : RecipeWorker
	{
		//
		// Fields
		//

		//
		// Methods
		//

		public override void ApplyOnPawn (Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients)
		{
			if(pawn.health.hediffSet.HasHediff(HediffDef.Named("HumanPregnancy"))){
				Hediff_HumanPregnancy preggo = (Hediff_HumanPregnancy)pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("HumanPregnancy"));
				preggo.DiscoverPregnancy();				
			}
			else{
				Messages.Message (billDoer.NameStringShort + " has determined " + pawn.NameStringShort + " is not pregnant.", MessageSound.Standard);
			}
		}
	}
}
