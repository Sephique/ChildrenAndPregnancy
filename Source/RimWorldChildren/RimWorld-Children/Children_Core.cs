using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;
using System.Collections.Generic;
using HugsLib;
using Harmony;
using System.Reflection.Emit;
using System.Linq;
using System;

namespace RimWorldChildren
{
	public class ChildrenBase : ModBase
	{
		public override string ModIdentifier {
			get {
				return "Children_and_Pregnancy";
			}
		}
	}

	public static class AgeStage
	{
		public const int Baby = 0;
		public const int Toddler = 1;
		public const int Child = 2;
		public const int Teenager = 3;
		public const int Adult = 4;
	}

	public static class ChildrenUtility{

		public static float ChildMaxWeaponMass(Pawn pawn){
			const float baseMass = 2.5f;
			return (pawn.skills.GetSkill (SkillDefOf.Shooting).Level * 0.1f) + baseMass;
		}

		public static bool CanBreastfeed(Pawn pawn)
		{
			if (pawn.gender != Gender.Female &&
				pawn.ageTracker.CurLifeStage.reproductive &&
				pawn.ageTracker.AgeBiologicalYears < 50 &&
				pawn.health.hediffSet.HasHediff (HediffDef.Named ("Lactating")))
				return true;
			else
				return false;
		}

		public static T FailOnBaby<T>(this T f) where T : IJobEndable{
			f.AddEndCondition (delegate {
				//				return JobCondition.Incompletable;
				if(f.GetActor().ageTracker.CurLifeStageIndex <= 1)
					return JobCondition.Incompletable;
				else
					return JobCondition.Ongoing;
			});
			return f;
		}

		internal static BodyPartRecord GetPawnBodyPart(Pawn pawn, String bodyPart)
		{
			return pawn.RaceProps.body.AllParts.Find (x => x.def == DefDatabase<BodyPartDef>.GetNamed(bodyPart, true));
		}
	}

	[StaticConstructorOnStartup]
	internal static class HarmonyPatches{

		static HarmonyPatches(){

			HarmonyInstance harmonyInstance = HarmonyInstance.Create ("rimworld.thirite.children_and_pregnancy");
			HarmonyInstance.DEBUG = true;

			var jobdriver_lovin_postfix = typeof(Lovin_Override).GetMethod ("JobDriver_Lovin_MoveNext_Postfix", AccessTools.all);
			harmonyInstance.Patch (typeof(JobDriver_Lovin).GetNestedTypes (AccessTools.all) [0].GetMethod ("MoveNext"), null, new HarmonyMethod (jobdriver_lovin_postfix), null);

			var jobdriver_wear_transpiler = typeof(Wear_Override).GetMethod ("JobDriver_Wear_MoveNext_Transpiler", AccessTools.all);
			harmonyInstance.Patch (typeof(JobDriver_Wear).GetNestedTypes (AccessTools.all) [0].GetMethod ("MoveNext"), null, null, new HarmonyMethod (jobdriver_wear_transpiler));

			var bed_floatoptions_movenext_transpiler = typeof(BedHarmonyPatches).GetMethod ("GetFloatMenuOptions_Transpiler", AccessTools.all);
			harmonyInstance.Patch (typeof(Building_Bed).GetNestedType("<GetFloatMenuOptions>c__Iterator155", AccessTools.all).GetMethod ("MoveNext"), null, null, new HarmonyMethod (bed_floatoptions_movenext_transpiler));
		}
	}

	[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new []{typeof(PawnGenerationRequest)})]
	public static class PawnGenerate_Patch
	{
		[HarmonyPostfix]
		internal static void _GeneratePawn (ref PawnGenerationRequest request, ref Pawn __result)
		{
			Pawn pawn = __result;
			// Children hediff being injected
			if (pawn.ageTracker.CurLifeStageIndex <= 2 && pawn.kindDef.race == ThingDefOf.Human) {
				// Clean out drug randomly generated drug addictions
				pawn.health.hediffSet.Clear ();
				pawn.health.AddHediff (HediffDef.Named ("BabyState"), null, null);
				if (pawn.Dead) {
					Log.Error (pawn.NameStringShort + " died on generation. This is caused by a mod conflict. Disable any conflicting mods before running Children & Pregnancy.");
				}
				Hediff_Baby babystate = (Hediff_Baby)pawn.health.hediffSet.GetFirstHediffOfDef (HediffDef.Named ("BabyState"));
				if (babystate != null) {
					for (int i = 0; i != pawn.ageTracker.CurLifeStageIndex + 1; i++) {
						babystate.GrowUpTo (i, true);
					}
				}
				if (pawn.ageTracker.CurLifeStageIndex == 2 && pawn.ageTracker.AgeBiologicalYears < 10) {
					pawn.story.childhood = BackstoryDatabase.allBackstories ["CustomBackstory_NA_Childhood"];
				}
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
	public static class JobGiver_OptimizeApparel_TryGiveJob_Patch{
		[HarmonyPostfix]
		internal static void TryGiveJob_Patch(ref Job __result, ref Pawn pawn){
			// Pawn is a toddler or baby
			if (pawn.ageTracker.CurLifeStageIndex <= 1) {
				__result = null;

			}
		}
	}
		
	[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
	public static class Pawn_HealtherTracker_ShouldBeDowned_Patch
	{
		[HarmonyPostfix]
		internal static void SBD(ref Pawn_HealthTracker __instance, ref bool __result){
			Pawn pawn = (Pawn)AccessTools.Field (typeof(Pawn_HealthTracker), "pawn").GetValue (__instance);
			if (pawn.RaceProps.Humanlike && pawn.ageTracker.CurLifeStageIndex <= 2) {
				__result = __instance.hediffSet.PainTotal > 0.4f || !__instance.capacities.CanBeAwake || !__instance.capacities.CapableOf (PawnCapacityDefOf.Moving);
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker), "CheckForStateChange")]
	public static class Pawn_HealthTracker_CheckForStateChange_Patch
	{
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CheckForStateChange_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> ILs = instructions.ToList ();

			MethodInfo KillPawn = typeof(Pawn).GetMethod ("Kill");
			int index = ILs.FindIndex (IL => IL.opcode == OpCodes.Callvirt && IL.operand == KillPawn);
			ILs.RemoveRange (index - 3, 4);

			foreach (CodeInstruction instruction in ILs) {
				yield return instruction;
			}
		}
	}

	[HarmonyPatch(typeof(RestUtility), "WakeThreshold")]
	public static class RestUtility_WakeThreshold_Patch{
		[HarmonyPostfix]
		internal static void WakeThreshold_Patch(ref float __result, ref Pawn p){
			if (p.ageTracker.CurLifeStageIndex < AgeStage.Child && p.health.hediffSet.HasHediff (HediffDef.Named ("UnhappyBaby"))) {
				// Babies wake up if they're unhappy
				__result = 0.15f;
			}
		}
	}

	[HarmonyPatch(typeof(Verb_Shoot), "TryCastShot")]
	public static class VerbShoot_TryCastShot_Patch{
		[HarmonyPostfix]
		internal static void TryCastShot_Patch(ref Verb_Shoot __instance){
			Pawn pawn = __instance.CasterPawn;
			if (pawn != null && pawn.ageTracker.CurLifeStageIndex <= AgeStage.Child) {
				// The weapon is too heavy and the child will (likely) drop it when trying to fire
				if (__instance.ownerEquipment.def.BaseMass > ChildrenUtility.ChildMaxWeaponMass(pawn)) {

					ThingWithComps benis;
					pawn.equipment.TryDropEquipment (__instance.ownerEquipment, out benis, pawn.Position, false);

					float recoilForce = (__instance.ownerEquipment.def.BaseMass - 3);

					if(recoilForce > 0){
						string[] hitPart = {
							"Torso",
							"LeftShoulder",
							"LeftArm",
							"LeftHand",
							"RightShoulder",
							"RightArm",
							"RightHand",
							"Head",
							"Neck",
							"LeftEye",
							"RightEye",
							"Nose", 
						};
						int hits = Rand.Range (1, 4);
						while (hits > 0) {
							pawn.TakeDamage (new DamageInfo (DamageDefOf.Blunt, (int)((recoilForce + Rand.Range (0f, 3f)) / hits), -1, __instance.ownerEquipment,
								ChildrenUtility.GetPawnBodyPart (pawn, hitPart.RandomElement<String> ()), null));
							hits--;
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded")]
	public static class PawnEquipmentracker_NotifyEquipmentAdded_Patch{
		[HarmonyPostfix]
		internal static void Notify_EquipmentAdded_Patch(ref ThingWithComps eq, ref Pawn_EquipmentTracker __instance){
			Pawn pawn = __instance.ParentHolder as Pawn;
			if (eq.def.BaseMass > ChildrenUtility.ChildMaxWeaponMass(pawn) && pawn.ageTracker.CurLifeStageIndex <= AgeStage.Child) {
				Messages.Message("MessageWeaponTooLarge".Translate(new object[]{eq.def.label, ((Pawn)__instance.ParentHolder).NameStringShort}),MessageSound.Negative );
			}
		}
	}
}

