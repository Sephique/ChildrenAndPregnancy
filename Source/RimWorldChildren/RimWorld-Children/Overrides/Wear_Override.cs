using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Harmony;
using System.Linq;

namespace RimWorldChildren
{
	internal static class Wear_Override
	{
		internal static IEnumerable<CodeInstruction> JobDriver_Wear_MoveNext_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> ILs = instructions.ToList ();

			MethodInfo failOnBaby = typeof(ChildrenUtility).GetMethod ("FailOnBaby", AccessTools.all).MakeGenericMethod (typeof(JobDriver_Wear));

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
	}
}

