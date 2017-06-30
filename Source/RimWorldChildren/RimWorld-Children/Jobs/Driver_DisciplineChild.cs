using System;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Diagnostics;

namespace RimWorldChildren
{
	public class JobDriver_DisciplineChild : JobDriver
	{
		//
		// Static Fields
		//
		private const int disciplineDuration = 100;

		//
		// Properties
		//
		protected Pawn Victim {
			get {
				return (Pawn)TargetA.Thing;
			}
		}

		internal static BodyPartRecord GetRandomDisciplinePart(Pawn victim)
		{
			List<BodyPartDef> parts = new List<BodyPartDef> {
				BodyPartDefOf.Torso,
				BodyPartDefOf.Head,
				BodyPartDefOf.LeftLeg,
				BodyPartDefOf.RightLeg,
				BodyPartDefOf.LeftArm,
				BodyPartDefOf.RightArm
			};
			return victim.RaceProps.body.AllParts.Find (x => x.def == (BodyPartDef)parts.RandomElement());
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull (TargetIndex.A);
			this.FailOnDowned (TargetIndex.A);
			yield return Toils_Goto.GotoThing (TargetIndex.A, PathEndMode.Touch);
			Toil prepare = new Toil();
			prepare.initAction = delegate
			{
				PawnUtility.ForceWait(Victim, disciplineDuration, Victim);
			};
			prepare.defaultCompleteMode = ToilCompleteMode.Delay;
			prepare.defaultDuration = disciplineDuration;
			yield return prepare;
			yield return new Toil
			{
				initAction = delegate
				{
					int amount = Rand.Range(1,2);
					Victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, amount, -1f, GetActor(), GetRandomDisciplinePart(Victim), null));
					this.AddEndCondition (() => JobCondition.Succeeded);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}


