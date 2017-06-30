using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;

namespace RimWorldChildren
{

	public class JobGiver_TendToBaby : ThinkNode_JobGiver
	{
		//
		// Methods
		//
		protected override Job TryGiveJob (Pawn pawn)
		{
			Pawn victim = null;
			if (pawn.gender != Gender.Female || !pawn.ageTracker.CurLifeStage.reproductive) {
				return null;
			}
			if(pawn.relations.ChildrenCount > 0){
				foreach (Pawn child in pawn.relations.Children) {
					if (child.needs.food.CurLevelPercentage < child.needs.food.PercentageThreshHungry || child.needs.joy.CurLevelPercentage < 0.1f) {
						victim = child;
						break;
					}
				}
			}
			//List<Pawn> babies = new List<Pawn> ();
			foreach (Pawn mapPawn in pawn.MapHeld.mapPawns.FreeColonistsAndPrisoners) {
				if (mapPawn.ageTracker.CurLifeStageIndex <= 1 &&
				    mapPawn.health.hediffSet.HasHediff (HediffDef.Named ("UnhappyBaby"))) {
						victim = mapPawn;
						break;
				}
			}

			if (victim != null) {
				Log.Message ("Found a victim for the BabyTending JobGiver");
				if(victim.needs.food.CurLevelPercentage < victim.needs.food.PercentageThreshHungry)
					return new Job (DefDatabase<JobDef>.GetNamed ("BreastFeedBaby"), victim);
				else
					return new Job (DefDatabase<JobDef>.GetNamed ("PlayWithBaby"), victim);
			}
			Log.Message ("Couldn't find a victim for the BabyTending JobGiver");
			return null;
		}
	}

	public class JobDriver_BreastFeedBaby : JobDriver
	{
		//
		// Static Fields
		//
		private const int breastFeedDuration = 300;

		//
		// Properties
		//
		protected Pawn Victim {
			get {
				return (Pawn)TargetA.Thing;
			}
		}

//		public override void ExposeData ()
//		{
//			base.ExposeData ();
//		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull (TargetIndex.A);
			this.FailOn(delegate {
				if(pawn.gender != Gender.Female && pawn.ageTracker.CurLifeStage.reproductive && pawn.ageTracker.AgeBiologicalYears < 50) return true;
				else return false;
			}
			);
			yield return Toils_Goto.GotoThing (TargetIndex.A, PathEndMode.Touch);
			Toil prepare = new Toil();
			prepare.initAction = delegate
			{
				PawnUtility.ForceWait(Victim, breastFeedDuration, Victim);
			};
			prepare.defaultCompleteMode = ToilCompleteMode.Delay;
			prepare.defaultDuration = breastFeedDuration;
			yield return prepare;
			yield return new Toil
			{
				initAction = delegate
				{
					AddEndCondition (() => JobCondition.Succeeded);
					// Baby is full
					Victim.needs.food.CurLevelPercentage = 100;
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}