using RimWorld;
using System;
using Verse;
using Verse.AI;
using Harmony;


namespace RimWorldChildren
{


	internal static class Lovin_Override
	{
		internal static void JobDriver_Lovin_MoveNext_Postfix(ref Toil __result, ref JobDriver_Lovin __instance){
			// Let's find the last yield return block
			JobDriver_Lovin _this = __instance;
			if (__result.socialMode == RandomSocialMode.Off) {
				__result.AddFinishAction (delegate {
					// one in five chance to become pregnant? Probably expand on this later
					// Make sure this isn't a gay/lesbian couple. Mpreg fujoshits blown the FUCK out
					Pawn partner = (Pawn)AccessTools.Property (typeof(JobDriver_Lovin), "Partner").GetValue (_this, null);
					if(_this.pawn.gender != partner.gender){
						// Find out who should become pregnant
						if(partner.gender == Gender.Female) Lovin_Override.TryToImpregnate(_this.pawn, partner);
						else Lovin_Override.TryToImpregnate(partner, _this.pawn);
					}
				});
			}
		}
			
		internal static void TryToImpregnate(Pawn male, Pawn female){

			// Only humans can be impregnated for now
			if (female.def.defName != "Human")
				return;

			BodyPartRecord torso = female.RaceProps.body.AllParts.Find (x => x.def == BodyPartDefOf.Torso);
			HediffDef contraceptive = HediffDef.Named ("Contraceptive");

			// Make sure the woman is not pregnanct and not using a contraceptive
			if(female.health.hediffSet.HasHediff(HediffDefOf.Pregnant, torso) || female.health.hediffSet.HasHediff(contraceptive, null) || male.health.hediffSet.HasHediff(contraceptive, null)){
				return;
			}
			// Check the pawn's age to see how likely it is she can carry a fetus
			// 25 and below is guaranteed, 50 and above is impossible, 37.5 is 50% chance
			float preg_chance = Math.Max (1 - (Math.Max (female.ageTracker.AgeBiologicalYearsFloat - 25, 0) / 25), 0) * 0.33f;
			if (preg_chance < Rand.Value) {
				//Log.Message ("Impregnation failed. Chance was " + preg_chance);
				return;
			}
			//Log.Message ("Impregnation succeeded. Chance was " + preg_chance);
			// Spawn a bunch of hearts. Sharp eyed players may notice this means impregnation occurred.
			for(int i = 0; i <= 3; i++){
				MoteMaker.ThrowMetaIcon(male.Position, male.MapHeld, ThingDefOf.Mote_Heart);
				MoteMaker.ThrowMetaIcon(female.Position, male.MapHeld, ThingDefOf.Mote_Heart);
			}

			// Do the actual impregnation. We apply it to the torso because Remove_Hediff in operations doesn't work on WholeBody (null body part)
			// for whatever reason.
			Hediff_HumanPregnancy hediff_Pregnant = (Hediff_HumanPregnancy)HediffMaker.MakeHediff (HediffDef.Named("HumanPregnancy"), female, torso);
			hediff_Pregnant.father = male;
			female.health.AddHediff (hediff_Pregnant, torso, null);
		}
	}

	[HarmonyPatch(typeof(JobGiver_DoLovin), "TryGiveJob")]
	public static class JobGiver_DoLovin_TryGiveJob_Patch{
		[HarmonyPostfix]
		internal static void TryGiveJob_Patch(ref Job __result, ref Pawn pawn){
			if (pawn.ageTracker != null && pawn.ageTracker.CurLifeStageIndex <= AgeStage.Child) {
				__result = null;
			}
		}
	}
}

