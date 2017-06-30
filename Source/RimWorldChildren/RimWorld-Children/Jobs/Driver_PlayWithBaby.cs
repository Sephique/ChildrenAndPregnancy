using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimWorldChildren
{
	public class JobDriver_PlayWithBaby : JobDriver
	{
		const TargetIndex BabyInd = TargetIndex.A;

		const TargetIndex ChairInd = TargetIndex.B;

		Pawn Baby
		{
			get
			{
				return (Pawn)CurJob.GetTarget(TargetIndex.A).Thing;
			}
		}

		Thing Chair
		{
			get
			{
				return CurJob.GetTarget(TargetIndex.B).Thing;
			}
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			this.FailOn (() => !Baby.InBed () || !Baby.Awake());
			if (Chair != null)
			{
				this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
			}
			yield return Toils_Reserve.Reserve (TargetIndex.A);
			if (Chair != null)
			{
				yield return Toils_Reserve.Reserve (TargetIndex.B);
				yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
			}
			else
			{
				yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			}
			yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
			yield return new Toil
			{
				tickAction = delegate
				{
					Baby.needs.joy.GainJoy(CurJob.def.joyGainRate * 0.000144f, CurJob.def.joyKind);
					if (pawn.IsHashIntervalTick(320))
					{
						InteractionDef intDef = (Rand.Value >= 0.8f) ? InteractionDefOf.DeepTalk : InteractionDefOf.Chitchat;
						pawn.interactions.TryInteractWith(Baby, intDef);
					}
					pawn.Drawer.rotator.FaceCell(Baby.Position);
					pawn.GainComfortFromCellIfPossible();
					JoyUtility.JoyTickCheckEnd (pawn, JoyTickFullJoyAction.None);
					if (pawn.needs.joy.CurLevelPercentage > 0.9999f && Baby.needs.joy.CurLevelPercentage > 0.9999f)
					{
						pawn.jobs.EndCurrentJob (JobCondition.Succeeded);
					}
				},
				socialMode = RandomSocialMode.Off,
				defaultCompleteMode = ToilCompleteMode.Delay,
				defaultDuration = CurJob.def.joyDuration
			};
		}
	}
}