using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;

namespace RimWorldChildren
{

	public class WorkGiver_BreastfeedBaby : WorkGiver_Scanner
	{
		//
		// Properties
		//
		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.Touch;
			}
		}
		public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup (ThingRequestGroup.Pawn);
			}
		}

		//
		// Methods
		//
		public override bool HasJobOnThing (Pawn pawn, Thing t, bool forced = false)
		{
			Pawn pawn2 = t as Pawn;
			// Make sure pawn can actually breastfeed before wasting time checking anything else
			if (!ChildrenUtility.CanBreastfeed (pawn))
				return false;
			if (pawn2 == null || pawn2 == pawn) {
				return false;
			}
			if (!pawn2.RaceProps.Humanlike) {
				return false;
			}
			if (pawn2.needs.food == null || pawn2.needs.food.CurLevelPercentage > pawn2.needs.food.PercentageThreshHungry + 0.02) {
				return false;
			}
			if (!FeedPatientUtility.ShouldBeFed (pawn2)) {
				return false;
			}
			if (!pawn.CanReserveAndReach (t, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced)) {
				return false;
			}
			return true;
		}

		public override Job JobOnThing (Pawn pawn, Thing t, bool forced = false)
		{
			Pawn pawn2 = (Pawn)t;
			if (pawn2 != null) {
				return new Job (DefDatabase<JobDef>.GetNamed ("BreastfeedBaby")) {
					targetA = pawn2,
				};
			}
			return null;
		}
	}

	public class JobDriver_BreastfeedBaby : JobDriver
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
			this.FailOnSomeonePhysicallyInteracting (TargetIndex.A);
			this.FailOn(delegate {
				if(!ChildrenUtility.CanBreastfeed (pawn) || !pawn.CanReserve (TargetA, 1, -1, null, false))
					return true;
				else return false;
			});

			yield return Toils_Reserve.Reserve (TargetIndex.A, 1, -1, null);
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