using RimWorld;
using System;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Harmony;
using UnityEngine;


namespace RimWorldChildren
{
	internal static class Lovin_Override
	{
		internal static readonly Func<JobDriver_Lovin, TargetIndex> _BedIndFA = FieldAccessor.GetFieldAccessor<JobDriver_Lovin, TargetIndex> ("BedInd");
		internal static readonly Func<JobDriver_Lovin, TargetIndex> _PartnerIndFA = FieldAccessor.GetFieldAccessor<JobDriver_Lovin, TargetIndex> ("PartnerInd");

		internal static IEnumerable<Toil> _MakeNewToils (this JobDriver_Lovin _this)
		{
			TargetIndex _BedInd = _BedIndFA (_this);
			TargetIndex _PartnerInd = _PartnerIndFA (_this);

			// Flags we use during reflection
			BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
			// Let's query the private members we need
			Pawn _Partner = (Pawn)typeof(JobDriver_Lovin).GetProperty ("Partner", flags).GetValue (_this, null);
			Building_Bed _Bed = (Building_Bed)typeof(JobDriver_Lovin).GetProperty ("Bed", flags).GetValue (_this, null);
			FieldInfo _ticksLeft = typeof(JobDriver_Lovin).GetField ("ticksLeft", flags);
			MethodInfo _GenerateRandomMinTicksToNextLovin = typeof(JobDriver_Lovin).GetMethod ("GenerateRandomMinTicksToNextLovin", flags);

			_this.FailOnDespawnedOrNull(_BedInd);
			_this.FailOnDespawnedOrNull(_PartnerInd);
			_this.FailOn(() => !_Partner.health.capacities.CanBeAwake);
			_this.KeepLyingDown(_BedInd);
			yield return Toils_Reserve.Reserve(_PartnerInd, 1, -1, null);
			yield return Toils_Reserve.Reserve(_BedInd, _Bed.SleepingSlotsCount, 0, null);
			yield return Toils_Bed.ClaimBedIfNonMedical(_BedInd, TargetIndex.None);
			yield return Toils_Bed.GotoBed(_BedInd);
			yield return new Toil
			{
				initAction = delegate
				{
					if (_Partner.CurJob == null || _Partner.CurJob.def != JobDefOf.Lovin)
					{
						Job newJob = new Job(JobDefOf.Lovin, _this.pawn, _Bed);
						_Partner.jobs.StartJob(newJob, JobCondition.InterruptForced, null, false, true, null, null);
						_ticksLeft.SetValue(_this, (int)(2500f * Mathf.Clamp(Rand.Range(0.1f, 1.1f), 0.1f, 2f)));
					}
					else
					{
						_ticksLeft.SetValue(_this, 9999999);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			Toil doLovin = Toils_LayDown.LayDown(_BedInd, true, false, false, false);
			doLovin.FailOn(() => _Partner.CurJob == null || _Partner.CurJob.def != JobDefOf.Lovin);
			doLovin.AddPreTickAction(delegate
				{
					_ticksLeft.SetValue(_this, (int)_ticksLeft.GetValue(_this) - 1);
					if ((int)_ticksLeft.GetValue(_this) <= 0)
					{
						_this.ReadyForNextToil();
					}
					else if (_this.pawn.IsHashIntervalTick(100))
					{
						MoteMaker.ThrowMetaIcon(_this.pawn.Position, _this.pawn.Map, ThingDefOf.Mote_Heart);
					}
				});
			doLovin.AddFinishAction(delegate
				{
					// one in five chance to become pregnant? Probably expand on this later
					// Make sure this isn't a gay/lesbian couple. Mpreg fujoshits blown the FUCK out
					if(_this.pawn.gender != _Partner.gender){
						// Find out who should become pregnant
						if(_Partner.gender == Gender.Female) TryToImpregnate(_this.pawn, _Partner);
							else TryToImpregnate(_Partner, _this.pawn);
					}

					Thought_Memory newThought = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
					_this.pawn.needs.mood.thoughts.memories.TryGainMemory(newThought, _Partner);
					_this.pawn.mindState.canLovinTick = Find.TickManager.TicksGame + (int)_GenerateRandomMinTicksToNextLovin.Invoke(_this, new object[] {_this.pawn});
				});
			doLovin.socialMode = RandomSocialMode.Off;
			yield return doLovin;
		}
			
		internal static void TryToImpregnate(Pawn male, Pawn female){
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

		// JobGiver_DoLovin.TryGiveJob override
		internal static Job _TryGiveJob (Pawn pawn)
		{
			// Only teenagers and up can do Lovin'
			if (pawn.ageTracker == null || pawn.ageTracker.CurLifeStageIndex <= 3) {
				return null;
			}
			
			if (Find.TickManager.TicksGame < pawn.mindState.canLovinTick) {
				return null;
			}
			if (pawn.CurJob == null) return null;
			//if (!pawn.jobs.curDriver.layingDown || pawn.jobs.curDriver.layingDownBed == null || pawn.jobs.curDriver.layingDownBed.Medical || !pawn.health.capacities.CanBeAwake) {
			if (pawn.CurrentBed () == null || pawn.CurrentBed ().Medical || !pawn.health.capacities.CanBeAwake) {
				return null;
			}
			Pawn partnerInMyBed = LovePartnerRelationUtility.GetPartnerInMyBed (pawn);
			if (partnerInMyBed == null) return null;
			if (!partnerInMyBed.health.capacities.CanBeAwake || Find.TickManager.TicksGame < partnerInMyBed.mindState.canLovinTick) {
				return null;
			}
			if (!pawn.CanReserve (partnerInMyBed, 1, -1, null, false) || !partnerInMyBed.CanReserve (pawn, 1, -1, null, false)) {
				return null;
			}
			pawn.mindState.awokeVoluntarily = true;
			partnerInMyBed.mindState.awokeVoluntarily = true;
			return new Job (JobDefOf.Lovin, partnerInMyBed, pawn.CurrentBed());
		}
	}
}

