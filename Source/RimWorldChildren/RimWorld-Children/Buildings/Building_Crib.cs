using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using System.Diagnostics;
using System.Text;
using System;
using Verse.AI;

namespace RimWorldChildren
{
	public class Building_Crib : Building, IAssignableBuilding
	{
		public Pawn owner = null;

		private static readonly Color SheetColorNormal = new Color(0.6313726f, 0.8352941f, 0.7058824f);

		public IEnumerable<Pawn> AssigningCandidates
		{
			get
			{
				if (!base.Spawned || Map.mapPawns.FreeColonists.Count (pawn => pawn.ageTracker.CurLifeStageIndex <= 2) == 0)
				{
					return Enumerable.Empty<Pawn>();
				}
				return Map.mapPawns.FreeColonists.Where(pawn => pawn.ageTracker.CurLifeStageIndex <= 2);
			}
		}

		public override Color DrawColor
		{
			get
			{
				if (def.MadeFromStuff)
				{
					return base.DrawColor;
				}
				return DrawColorTwo;
			}
		}

		public override Color DrawColorTwo
		{
			get
			{
				return SheetColorNormal;
			}
		}

		[DebuggerHidden]
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo g in base.GetGizmos())
			{
				yield return g;
			}
			if (this.def.building.bed_humanlike && base.Faction == Faction.OfPlayer)
			{

				yield return new Command_Action
				{
					defaultLabel = "CommandBedSetOwnerLabel".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", true),
					defaultDesc = "CommandBedSetOwnerDesc".Translate(),
					action = delegate
					{
						Find.WindowStack.Add(new Dialog_AssignBuildingOwner(this));
					},
					hotKey = KeyBindingDefOf.Misc3
				};
			}
		}

		public void TryAssignPawn (Pawn pawn)
		{
			// Crib is already assigned to this baby
			if (owner == pawn)
				return;
			pawn.ownership.UnclaimBed ();
			owner.ownership.UnclaimBed ();
			owner = pawn;

		}

		public void TryUnassignPawn (Pawn pawn)
		{
			if (owner == pawn)
				owner = null;
		}

		public IEnumerable<Pawn> AssignedPawns {
			get {
				List<Pawn> ownerList = new List<Pawn>();
				ownerList.Add (owner);
				return ownerList;
			}
		}

		public int MaxAssignedPawnsCount {
			get {
				return 1;
			}
		}
	}
}

