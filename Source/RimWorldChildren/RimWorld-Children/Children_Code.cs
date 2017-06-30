using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using HugsLib;
using Harmony;

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

//	[StaticConstructorOnStartup]
//	internal static class HarmonyPatches{
//
//		static HarmonyPatches(){
//
//			HarmonyInstance harmonyInstance = HarmonyInstance.Create ("rimworld.thirite.childrenandpregnancy");
//			HarmonyInstance.DEBUG = true;
//			//harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
//		}
//	}

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

	public static class ChildCode
	{
		internal static BindingFlags UBF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		// Is this even a good idea to detour? I don't know.
		internal static object _SetNextOptimizeTick (this JobGiver_OptimizeApparel _this, Pawn pawn) {
			return typeof(JobGiver_OptimizeApparel).GetMethod ("SetNextOptimizeTick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke (_this, new object[] {pawn});
		}
		internal static Job _TryGiveJob (this JobGiver_OptimizeApparel _this, Pawn pawn)
		{
			if (pawn.ageTracker.CurLifeStageIndex <= 1) {
				// The pawn is a toddler or newborn and so won't try to wear clothes
				return null;
			}

			StringBuilder _debugSb = null;

			if (pawn.outfits == null) {
				Log.ErrorOnce (pawn + " tried to run JobGiver_OptimizeApparel without an OutfitTracker", 5643897);
				return null;
			}
			if (pawn.Faction != Faction.OfPlayer) {
				Log.ErrorOnce ("Non-colonist " + pawn + " tried to optimize apparel.", 764323);
				return null;
			}
			if (!DebugViewSettings.debugApparelOptimize) {
				if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick) {
					return null;
				}
			}
			else {
				_debugSb = new StringBuilder ();
				_debugSb.AppendLine (string.Concat (new object[] {
					"Scanning for ",
					pawn,
					" at ",
					pawn.Position
				}));
			}
			Outfit currentOutfit = pawn.outfits.CurrentOutfit;
			List<Apparel> wornApparel = pawn.apparel.WornApparel;
			for (int i = wornApparel.Count - 1; i >= 0; i--) {
				if (!currentOutfit.filter.Allows (wornApparel [i]) && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop (wornApparel [i])) {
					return new Job (JobDefOf.RemoveApparel, wornApparel [i]) {
						haulDroppedApparel = true
					};
				}
			}
			Thing thing = null;
			float num = 0;
			List<Thing> list = pawn.Map.listerThings.ThingsInGroup (ThingRequestGroup.Apparel);
			if (list.Count == 0) {
				_SetNextOptimizeTick (_this, pawn);
				return null;
			}

			//_neededWarmth(_this) = PawnApparelGenerator.CalculateNeededWarmth (pawn, pawn.Map, GenLocalDate.Month (pawn));
			FieldInfo _neededWarmth = typeof(JobGiver_OptimizeApparel).GetField ("neededWarmth", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			if (_neededWarmth != null)
				//_neededWarmth.SetValue(_this, PawnApparelGenerator.CalculateNeededWarmth (pawn, pawn.Map, GenLocalDate.Month (pawn))); 
				_neededWarmth.SetValue (_this, PawnApparelGenerator.CalculateNeededWarmth (pawn, pawn.Tile, GenLocalDate.Twelfth (pawn.Map)));

			for (int j = 0; j < list.Count; j++) {
				Apparel apparel = (Apparel)list [j];
				if (currentOutfit.filter.Allows (apparel)) {
					if (apparel.Map.slotGroupManager.SlotGroupAt (apparel.Position) != null) {
						if (!apparel.IsForbidden (pawn)) {
							float num2 = JobGiver_OptimizeApparel.ApparelScoreGain (pawn, apparel);
							if (DebugViewSettings.debugApparelOptimize) {
								_debugSb.AppendLine (apparel.LabelCap + ": " + num2.ToString ("F2"));
							}
							if (num2 >= 0.05 && num2 >= num) {
								if (ApparelUtility.HasPartsToWear (pawn, apparel.def)) {
									if (pawn.CanReserveAndReach (apparel, PathEndMode.OnCell, pawn.NormalMaxDanger (), 1)) {
										thing = apparel;
										num = num2;
									}
								}
							}
						}
					}
				}
			}
			if (DebugViewSettings.debugApparelOptimize) {
				_debugSb.AppendLine ("BEST: " + thing);
				Log.Message (_debugSb.ToString ());
			}
			if (thing == null) {
				_SetNextOptimizeTick (_this, pawn);
				return null;
			}
			return new Job (JobDefOf.Wear, thing);
		}

		internal static bool _ShouldBeDowned (this Pawn_HealthTracker _this)
		{
			Pawn _pawn = (Pawn)typeof(Pawn_HealthTracker).GetField ("pawn", UBF).GetValue (_this);
			if (_pawn.RaceProps.Humanlike) {
				return _this.InPainShock || (_this.hediffSet.PainTotal > 0.4f && _pawn.ageTracker.CurLifeStageIndex <= 2) || !_this.capacities.CanBeAwake || !_this.capacities.CapableOf (PawnCapacityDefOf.Moving);
			}
			else
				return _this.InPainShock || !_this.capacities.CanBeAwake || !_this.capacities.CapableOf (PawnCapacityDefOf.Moving);
		}

		// CheckForStateChange from Pawn_HealthTracker
		internal static void _CheckForStateChange (this Pawn_HealthTracker _this, DamageInfo? dinfo, Hediff hediff)
		{
			if (!_this.Dead) {
				MethodInfo _ShouldBeDead = typeof(Pawn_HealthTracker).GetMethod ("ShouldBeDead", UBF);
				MethodInfo _ShouldBeDowned = typeof(Pawn_HealthTracker).GetMethod ("ShouldBeDowned", UBF);
				MethodInfo _MakeDowned = typeof(Pawn_HealthTracker).GetMethod ("MakeDowned", UBF);
				MethodInfo _MakeUndowned = typeof(Pawn_HealthTracker).GetMethod ("MakeUndowned", UBF);

				// Pointer to the pawn
				Pawn _pawn = (Pawn)typeof(Pawn_HealthTracker).GetField ("pawn", UBF).GetValue(_this);

				if ((bool)_ShouldBeDead.Invoke(_this, new object[]{})) {
					if (!_pawn.Destroyed) {
						bool flag = PawnUtility.ShouldSendNotificationAbout (_pawn);
						Caravan caravan = _pawn.GetCaravan ();
						_pawn.Kill (dinfo);
						if (flag) {
							_this.NotifyPlayerOfKilled (dinfo, hediff, caravan);
						}
					}
					return;
				}
				if (!_this.Downed) {
					if ((bool)_ShouldBeDowned.Invoke(_this, new object[]{})) {
						float num = (!_pawn.RaceProps.Animal) ? 0.67f : 0.47f;
						if (!_this.forceIncap && (_pawn.Faction == null || !_pawn.Faction.IsPlayer) && !_pawn.IsPrisonerOfColony && _pawn.RaceProps.IsFlesh && Rand.Value < num) {
							// Fug you
							//_this.Kill (dinfo, null);
							return;
						}
						_this.forceIncap = false;
						//_this.MakeDowned (dinfo, hediff);
						_MakeDowned.Invoke (_this, new object[] { dinfo, hediff });
						return;
					} else {
						if (!_this.capacities.CapableOf (PawnCapacityDefOf.Manipulation)) {
							if (_pawn.carryTracker != null && _pawn.carryTracker.CarriedThing != null && _pawn.jobs != null && _pawn.CurJob != null) {
								_pawn.jobs.EndCurrentJob (JobCondition.InterruptForced, true);
							}
							if (_pawn.equipment != null && _pawn.equipment.Primary != null) {
								if (_pawn.InContainerEnclosed) {
									_pawn.equipment.TryTransferEquipmentToContainer (_pawn.equipment.Primary, _pawn.holdingOwner);
								}
								else {
									if (_pawn.SpawnedOrAnyParentSpawned) {
										ThingWithComps thingWithComps;
										_pawn.equipment.TryDropEquipment (_pawn.equipment.Primary, out thingWithComps, _pawn.PositionHeld, true);
									}
									else {
										_pawn.equipment.DestroyEquipment (_pawn.equipment.Primary);
									}
								}
							}
						}
					}
				} else {
					//if (!this.ShouldBeDowned ()) {
					if (!(bool)_ShouldBeDowned.Invoke(_this, new object[]{})){
						_MakeUndowned.Invoke (_this, new object[]{});
						return;
					}
				}
			}
		}

	}
}

