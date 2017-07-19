using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;
using System.Collections.Generic;
using HugsLib;
using Harmony;
using System.Reflection.Emit;
using System.Linq;

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

	[StaticConstructorOnStartup]
	internal static class HarmonyPatches{

		static HarmonyPatches(){

			HarmonyInstance harmonyInstance = HarmonyInstance.Create ("rimworld.thirite.childrenandpregnancy");
			HarmonyInstance.DEBUG = true;
			//harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
			var jobdriver_lovin_postfix = typeof(HarmonyPatches).GetMethod ("JobDriver_Lovin_MoveNext_Postfix", AccessTools.all);
			harmonyInstance.Patch (typeof(JobDriver_Lovin).GetNestedTypes (AccessTools.all) [0].GetMethod("MoveNext"), null, new HarmonyMethod(jobdriver_lovin_postfix), null);
			var jobdriver_wear_transpiler = typeof(HarmonyPatches).GetMethod ("JobDriver_Wear_MoveNext_Transpiler", AccessTools.all);
			harmonyInstance.Patch (typeof(JobDriver_Wear).GetNestedTypes (AccessTools.all) [0].GetMethod("MoveNext"), null, null, new HarmonyMethod(jobdriver_wear_transpiler));
		}

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

		
		internal static IEnumerable<CodeInstruction> JobDriver_Wear_MoveNext_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> ILs = instructions.ToList ();


			MethodInfo failOnBaby = typeof(HarmonyPatches).GetMethod ("FailOnBaby", AccessTools.all).MakeGenericMethod (typeof(JobDriver_Wear));

			int index = ILs.FindIndex (x => x.labels.Count > 0);

			CodeInstruction newJump2 = new CodeInstruction (OpCodes.Ldarg_0);
			newJump2.labels.Add (ILs [index].labels [0]);
			ILs [index].labels.Clear ();
			ILs.Insert(index, newJump2);
			index++;
			ILs.Insert(index, new CodeInstruction(OpCodes.Ldfld, typeof(JobDriver_Wear).GetNestedType("<MakeNewToils>c__Iterator54", AccessTools.all).GetField("<>f__this", AccessTools.all) ) );
			index++;
			ILs.Insert(index, new CodeInstruction(OpCodes.Call, failOnBaby));
			index++;
			ILs.Insert(index, new CodeInstruction(OpCodes.Pop));

			foreach (CodeInstruction IL in ILs)
				yield return IL;
		}
		public static T FailOnBaby<T>(this T f) where T : IJobEndable{
			Log.Message ("Got here");
			f.AddEndCondition (delegate {
//				return JobCondition.Incompletable;
				if(f.GetActor().ageTracker.CurLifeStageIndex <= 1)
					return JobCondition.Incompletable;
				else
					return JobCondition.Ongoing;
			});
			return f;
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
}

