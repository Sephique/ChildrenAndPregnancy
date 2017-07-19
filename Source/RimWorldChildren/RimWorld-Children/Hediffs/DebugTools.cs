using RimWorld;
using Verse.AI;
using Verse;
using Harmony;

namespace RimWorldChildren
{
	// Makes the pawn do Lovin' if they're in bed with their partner
	public class Hediff_GetFuckin : HediffWithComps
	{
		public override void Tick ()
		{
			if (pawn.InBed ()) {
				Job lovin = (Job)AccessTools.Method (typeof(JobGiver_DoLovin), "TryGiveJob").Invoke (pawn, null);
				if (lovin != null) {
					pawn.jobs.StopAll (true);
					pawn.jobs.StartJob (lovin, JobCondition.InterruptForced, null, false, true, null);
				}
			}
			pawn.health.RemoveHediff (this);
		}
	}

	// Makes the pawn pregnant (if not already) and sets the pregnancy to near its end
	public class Hediff_MakePregnateLate :HediffWithComps
	{
		public override void Tick ()
		{
			if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("HumanPregnancy"))) {
				pawn.health.AddHediff (HediffDef.Named("HumanPregnancy"), pawn.RaceProps.body.AllParts.Find(x => x.def == BodyPartDefOf.Torso), null);
			}
			pawn.health.hediffSet.GetFirstHediffOfDef (HediffDef.Named("HumanPregnancy")).Severity = 0.995f;

			pawn.health.RemoveHediff (this);
		}
	}

	// Play with baby
	public class Hediff_GiveJobTest : HediffWithComps
	{
		public override void Tick ()
		{
			Pawn baby = null;
			// Try to find a baby
			foreach(Pawn colonist in pawn.Map.mapPawns.FreeColonists){
				if (colonist.ageTracker.CurLifeStageIndex == 0)
					baby = colonist;
			}
			if (baby != null) {
				Job playJob = new Job (DefDatabase<JobDef>.GetNamed ("BreastFeedBaby"), baby);
				//pawn.QueueJob (playJob);
				pawn.jobs.StartJob (playJob, JobCondition.InterruptForced, null, true, true, null, null);
				Log.Message ("Found baby " + baby.NameStringShort + " and proceeding to breastfeed.");
			} else
				Log.Message ("Failed to find any baby.");

			pawn.health.RemoveHediff (this);
		}
	}
}