using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace RimWorldChildren
{
	internal static class Wear_Override
	{

		internal static readonly Func<JobDriver_Wear, Pawn> _pawn = FieldAccessor.GetFieldAccessor<JobDriver_Wear, Pawn> ("pawn");

		internal static IEnumerable<Toil> _MakeNewToils(this JobDriver_Wear _this)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			PropertyInfo _CurJob = typeof(JobDriver_Wear).GetProperty("CurJob", flags);
			PropertyInfo _TargetThingA = typeof(JobDriver_Wear).GetProperty ("TargetThingA", flags);

			_this.FailOn(() => _pawn(_this).ageTracker.CurLifeStageIndex <= 1);
			yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
			Toil gotoApparel = new Toil();
			gotoApparel.initAction = delegate
			{
				_pawn(_this).pather.StartPath((Thing)_TargetThingA.GetValue(_this, null), PathEndMode.ClosestTouch);
			};
			gotoApparel.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			gotoApparel.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return gotoApparel;
			Toil prepare = new Toil();
			prepare.defaultCompleteMode = ToilCompleteMode.Delay;
			prepare.defaultDuration = 60;
			prepare.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
			prepare.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return prepare;
			yield return new Toil
			{
				initAction = delegate
				{
					Apparel apparel = (Apparel)((Job)_CurJob.GetValue(_this, null)).targetA.Thing;
					_pawn(_this).apparel.Wear(apparel, true);
					if (_pawn(_this).outfits != null && ((Job)_CurJob.GetValue(_this, null)).playerForced)
					{
						_pawn(_this).outfits.forcedHandler.SetForced(apparel, true);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}

