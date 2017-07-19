using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Harmony;

namespace RimWorldChildren
{
	[HarmonyPatch(typeof(ThoughtUtility), "CanGetThought")]
	public static class ThoughtUtility_CanGetThought_Patch{
		[HarmonyPostfix]
		internal static void CanGetThought_Patch(ref Pawn pawn, ref ThoughtDef def, ref bool __result)
		{
			// Toddlers and younger can't get these thoughts
			if (pawn.ageTracker.CurLifeStageIndex <= 1) {
				List<ThoughtDef> thoughtlist = new List<ThoughtDef>{
					ThoughtDefOf.AteWithoutTable,
					ThoughtDefOf.KnowPrisonerDiedInnocent,
					ThoughtDefOf.KnowPrisonerSold,
					ThoughtDefOf.Naked,
					ThoughtDefOf.SleepDisturbed,
					ThoughtDefOf.SleptOnGround,
					ThoughtDef.Named("CabinFever"),
					//ThoughtDef.Named("SharedBedroom")
				};

				foreach (ThoughtDef thought in thoughtlist) {
					if (def == thought)
						__result = false;
				}
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_SocialFighting), "TryGiveJob")]
	public static class JobGiver_SocialFighting_TryGiveJob_Patch
	{
		[HarmonyPostfix]
		internal static void TryGiveJob_Postfix(ref Pawn pawn, ref Job __result){
			Pawn other = ((MentalState_SocialFighting)pawn.MentalState).otherPawn;
			if (__result != null) {
				// Make sure kids don't start social fights with adults
				if (other.ageTracker.CurLifeStageIndex > 2 && pawn.ageTracker.CurLifeStageIndex <= 2) {
					Log.Message ("Debug: Child starting social fight with adult");
					// Adult will "start" the fight, following the code below
					other.interactions.StartSocialFight (pawn);
					__result = null;
				}

				// Make sure adults don't start social fights with kids (unless psychopaths)
				if (other.ageTracker.CurLifeStageIndex <= 2 && pawn.ageTracker.CurLifeStageIndex > 2 && !pawn.story.traits.HasTrait (TraitDefOf.Psychopath)) {
					//Log.Message ("Debug: Adult starting social fight with child");
					// If the pawn is not in a bad mood or is kind, they'll just tell them off
					if (pawn.story.traits.HasTrait (TraitDefOf.Kind) || pawn.needs.mood.CurInstantLevel > 0.45f || pawn.story.WorkTagIsDisabled(WorkTags.Violent)) {
						//Log.Message ("Debug: Adult has decided to tell off the child");
						JobDef chastise = DefDatabase<JobDef>.GetNamed ("ScoldChild", true);
						__result = new Job (chastise, other);
					}
					// Otherwise the adult will smack the child around
					else if (other.health.summaryHealth.SummaryHealthPercent > 0.93f) {
						//Log.Message ("Debug: Adult has decided to smack the child around, child health at " + other.health.summaryHealth.SummaryHealthPercent);
						JobDef paddlin = DefDatabase<JobDef>.GetNamed ("DisciplineChild", true);
						__result = new Job (paddlin, other);
					}

					pawn.MentalState.RecoverFromState ();
					__result = null;
				}
			}
		}
	}


//	internal static class Thought_Override
//	{
//		internal static bool _CanGetThought (Pawn pawn, ThoughtDef def)
//		{
//			ProfilerThreadCheck.BeginSample ("CanGetThought()");
//			try {
//				// Toddlers and younger can't get these thoughts
//				if (pawn.ageTracker.CurLifeStageIndex <= 1) {
//					List<ThoughtDef> thoughtlist = new List<ThoughtDef>{
//						ThoughtDefOf.AteWithoutTable,
//						ThoughtDefOf.KnowPrisonerDiedInnocent,
//						ThoughtDefOf.KnowPrisonerSold,
//						ThoughtDefOf.Naked,
//						ThoughtDefOf.SleepDisturbed,
//						ThoughtDefOf.SleptOnGround,
//						ThoughtDef.Named("CabinFever"),
//						//ThoughtDef.Named("SharedBedroom")
//					};
//
//					foreach (ThoughtDef thought in thoughtlist) {
//						if (def == thought)
//							return false;
//					}
//				}
//
//				if (!def.validWhileDespawned && !pawn.Spawned && !def.IsMemory) {
//					bool result = false;
//					return result;
//				}
//				if (def.nullifyingTraits != null) {
//					for (int i = 0; i < def.nullifyingTraits.Count; i++) {
//						if (pawn.story.traits.HasTrait (def.nullifyingTraits [i])) {
//							bool result = false;
//							return result;
//						}
//					}
//				}
//				if (!def.requiredTraits.NullOrEmpty<TraitDef> ()) {
//					bool flag = false;
//					for (int j = 0; j < def.requiredTraits.Count; j++) {
//						if (pawn.story.traits.HasTrait (def.requiredTraits [j])) {
//							if (!def.RequiresSpecificTraitsDegree || def.requiredTraitsDegree == pawn.story.traits.DegreeOfTrait (def.requiredTraits [j])) {
//								flag = true;
//								break;
//							}
//						}
//					}
//					if (!flag) {
//						bool result = false;
//						return result;
//					}
//				}
//				if (def.nullifiedIfNotColonist && !pawn.IsColonist) {
//					bool result = false;
//					return result;
//				}
//				if (ThoughtUtility.IsSituationalThoughtNullifiedByHediffs (def, pawn)) {
//					bool result = false;
//					return result;
//				}
//				if (ThoughtUtility.IsThoughtNullifiedByOwnTales (def, pawn)) {
//					bool result = false;
//					return result;
//				}
//			}
//			finally {
//				ProfilerThreadCheck.EndSample ();
//			}
//			return true;
//		}



//		internal static Job _TryGiveJob_SocialFight (this JobGiver_SocialFighting _this, Pawn pawn)
//		{
//			Pawn other = null;
//
//			if (pawn.mindState.mentalStateHandler.InMentalState) {
//				other = ((MentalState_SocialFighting)(pawn.mindState.mentalStateHandler.CurState)).otherPawn;
//			} else {
//				Log.Error ("Pawn trying to do job Social Fighting but not in Mental Break for Social Fighting!");
//				return null;
//			}
//
//			// Make sure kids don't start social fights with adults
//			if (other.ageTracker.CurLifeStageIndex > 2 && pawn.ageTracker.CurLifeStageIndex <= 2) {
//				Log.Message ("Debug: Child starting social fight with adult");
//				// Adult will "start" the fight, following the code below
//				other.interactions.StartSocialFight (pawn);
//				return null;
//			}
//
//			// Make sure adults don't start social fights with kids (unless psychopaths)
//			if (other.ageTracker.CurLifeStageIndex <= 2 && pawn.ageTracker.CurLifeStageIndex > 2 && !pawn.story.traits.HasTrait (TraitDefOf.Psychopath)) {
//
//				//Log.Message ("Debug: Adult starting social fight with child");
//
//				// If the pawn is not in a bad mood or is kind, they'll just tell them off
//				if (pawn.story.traits.HasTrait (TraitDefOf.Kind) || pawn.needs.mood.CurInstantLevel > 0.45f || pawn.story.WorkTagIsDisabled(WorkTags.Violent)) {
//					//Log.Message ("Debug: Adult has decided to tell off the child");
//					JobDef chastise = DefDatabase<JobDef>.GetNamed ("ScoldChild", true);
//					return new Job (chastise, other);
//				}
//				// Otherwise the adult will smack the child around
//				else if (other.health.summaryHealth.SummaryHealthPercent > 0.93f) {
//					//Log.Message ("Debug: Adult has decided to smack the child around, child health at " + other.health.summaryHealth.SummaryHealthPercent);
//					JobDef paddlin = DefDatabase<JobDef>.GetNamed ("DisciplineChild", true);
//					return new Job (paddlin, other);
//
//				}
//
//				pawn.MentalState.RecoverFromState ();
//				return null;
//			}
//
//			Verb verbToUse;
//			if (!InteractionUtility.TryGetRandomVerbForSocialFight (pawn, out verbToUse)) {
//				return null;
//			}
//			return new Job (JobDefOf.SocialFight, other) {
//				maxNumMeleeAttacks = 1,
//				verbToUse = verbToUse
//			};
//		}
//	}
}

