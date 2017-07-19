using System;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace RimWorldChildren
{
	[HarmonyPatch(typeof(Pawn_ApparelTracker), "ApparelChanged")]
	public static class Pawn_ApparelTracker_ApparelChanged_Patch{
		[HarmonyPostfix]
		internal static void ApparelChanged_Postfix(ref Pawn_ApparelTracker __instance){
			Pawn_ApparelTracker _this = __instance;
			LongEventHandler.ExecuteWhenFinished (delegate {
				Children_Drawing.ResolveAgeGraphics (_this.pawn.Drawer.renderer.graphics);
			});
		}
	}

	[HarmonyPatch(typeof(PawnGraphicSet), "ResolveAllGraphics")]
	public static class PawnGraphicSet_ResolveAllGraphics_Patch{
		[HarmonyPostfix]
		internal static void ResolveAllGraphics_Patch(ref PawnGraphicSet __instance){
			Pawn pawn = __instance.pawn;
			if (pawn.RaceProps.Humanlike) {
				Children_Drawing.ResolveAgeGraphics (__instance);
				__instance.ResolveApparelGraphics ();
			}
		}
	}

	[HarmonyPatch(typeof(PawnGraphicSet), "ResolveApparelGraphics")]
	public static class PawnGraphicSet_ResolveApparelGraphics_Patch{
		[HarmonyPrefix]
		internal static void ResolveApparelGraphics_Patch(ref PawnGraphicSet __instance){
			Pawn pawn = __instance.pawn;
			// Updates the beard
			if (pawn.apparel.BodyPartGroupIsCovered (BodyPartGroupDefOf.UpperHead) && pawn.RaceProps.Humanlike) {
				Children_Drawing.ResolveAgeGraphics (__instance);
			}
		}
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> ResolveApparelGraphics_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> ILs = instructions.ToList ();
			int injectIndex = ILs.FindIndex (x => x.opcode == OpCodes.Ldloca_S) - 4;
			ILs.RemoveRange (injectIndex + 2, 2);
			MethodInfo childBodyCheck = typeof(Children_Drawing).GetMethod ("ModifyChildBodyType");
			ILs.Insert(injectIndex + 2,new CodeInstruction(OpCodes.Call, childBodyCheck));

			foreach(CodeInstruction IL in ILs){
				yield return IL;
			}
		}
	}
	[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal", new [] { typeof(Vector3), typeof(Quaternion), typeof(Boolean), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(Boolean), typeof(Boolean)})]
	public static class PawnRenderer_RenderPawnInternal_Patch{
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> RenderPawnInternal_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILgen)
		{
			List<CodeInstruction> ILs = instructions.ToList ();

			// Change the root location of the child's draw position
			int injectIndex0 = ILs.FindIndex (x => x.opcode == OpCodes.Ldarg_1) + 1;
			List<CodeInstruction> injection0 = new List<CodeInstruction> {
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
				new CodeInstruction(OpCodes.Ldarg_S, 7),
				new CodeInstruction(OpCodes.Call, typeof(Children_Drawing).GetMethod("ModifyChildYPosOffset")),
			};
			ILs.InsertRange (injectIndex0, injection0);

			// Ensure pawn is a child or higher before drawing head
			int injectIndex1 = ILs.FindIndex (x => x.opcode == OpCodes.Ldfld && x.operand == AccessTools.Field (typeof(PawnGraphicSet), "headGraphic")) + 2;
			List<CodeInstruction> injection1 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldloc_0),
				new CodeInstruction (OpCodes.Ldfld, AccessTools.Field (typeof(Pawn), "ageTracker")),
				new CodeInstruction (OpCodes.Call, AccessTools.Property (typeof(Pawn_AgeTracker), "CurLifeStageIndex").GetGetMethod ()),
				new CodeInstruction (OpCodes.Ldc_I4_1),
				new CodeInstruction (OpCodes.Blt, ILs [injectIndex1 - 1].operand),
			};
			ILs.InsertRange (injectIndex1, injection1);

			// Modify the scale of a hat graphic when worn by a child
			int injectIndex2 = ILs.GetRange(injectIndex1, ILs.Count - injectIndex1).FindIndex (x => x.opcode == OpCodes.Stloc_S && x.operand is LocalBuilder && ((LocalBuilder)x.operand).LocalIndex == 16) + 1;
			List<CodeInstruction> injection2 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldloc_S, 16),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
				new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod("ModifyHatForChild")),
				new CodeInstruction (OpCodes.Stloc_S, 16),
			};
			ILs.InsertRange (injectIndex2, injection2);

			// Modify the scale of a hair graphic when worn by a child
			int injectIndex3 = ILs.FindIndex (x => x.opcode == OpCodes.Callvirt && x.operand == AccessTools.Method (typeof(PawnGraphicSet), "HairMatAt")) + 2;
			List<CodeInstruction> injection3 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldloc_S, 18),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
				new CodeInstruction (OpCodes.Call, AccessTools.Method(typeof(Children_Drawing), "ModifyHairForChild")),
				new CodeInstruction (OpCodes.Stloc_S, 18),
			};
			ILs.InsertRange (injectIndex3, injection3);

			// Modify the scale of clothing graphics when worn by a child
			int injectIndex4 = ILs.FindIndex (x => x.opcode == OpCodes.Stloc_S && x.operand is LocalBuilder && ((LocalBuilder)x.operand).LocalIndex == 4) + 1;
			List<CodeInstruction> injection4 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldloc_S, 4),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField ("pawn", AccessTools.all)),
				new CodeInstruction (OpCodes.Ldarg_S, 4),
				new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod ("ModifyClothingForChild")),
				new CodeInstruction (OpCodes.Stloc_S, 4),
			};
			ILs.InsertRange (injectIndex4, injection4);

			// Replace all secondary ldarg.1 with ldloc.1
			// This ensures they use the offset vector3 for children's height
			int firstLdarg1 = ILs.FindIndex (x => x.opcode == OpCodes.Ldarg_1) + 1;
			foreach (CodeInstruction IL in ILs.GetRange (firstLdarg1, ILs.Count - firstLdarg1).FindAll (x => x.opcode == OpCodes.Ldarg_1)) {
				IL.opcode = OpCodes.Ldloc_1;
			};

			foreach (CodeInstruction IL in ILs) {
				yield return IL;
			}
		}
	}

	public static class Children_Drawing
	{
		internal static void ResolveAgeGraphics(PawnGraphicSet graphics){
			LongEventHandler.ExecuteWhenFinished (delegate {

				if (!graphics.pawn.RaceProps.Humanlike) {
					return;
				}

				// Beards
				String beard = "";
				if (graphics.pawn.story.hairDef != null) {
					if (graphics.pawn.story.hairDef.hairTags.Contains ("Beard")) {
						if (graphics.pawn.apparel.BodyPartGroupIsCovered (BodyPartGroupDefOf.UpperHead) && !graphics.pawn.story.hairDef.hairTags.Contains ("DrawUnderHat")) {
							beard = "_BeardOnly";
						}
						if (graphics.pawn.ageTracker.CurLifeStageIndex <= AgeStage.Teenager) {
							graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> (DefDatabase<HairDef>.GetNamed ("Mop").texPath, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
						} else
							graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> (graphics.pawn.story.hairDef.texPath + beard, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
					} else
						graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> (graphics.pawn.story.hairDef.texPath, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
				}

				// Reroute the graphics for children
				// For babies and toddlers
				if (graphics.pawn.ageTracker.CurLifeStageIndex <= AgeStage.Baby) {
					string toddler_hair = "Boyish";
					if (graphics.pawn.gender == Gender.Female) {
						toddler_hair = "Girlish";
					}
					graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> ("Things/Pawn/Humanlike/Children/Hairs/Child_" + toddler_hair, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
					graphics.headGraphic = GraphicDatabase.Get<Graphic_Multi> ("Things/Pawn/Humanlike/null", ShaderDatabase.Cutout, Vector2.one, Color.white);
				}

				// The pawn is a baby
				if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Baby) {
					graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Single> ("Things/Pawn/Humanlike/Children/Bodies/Newborn", ShaderDatabase.CutoutSkin, Vector2.one, graphics.pawn.story.SkinColor);
				}

				// The pawn is a toddler
				if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Toddler) {
					string upright = "";
					if (graphics.pawn.ageTracker.AgeBiologicalYears >= AgeStage.Child) {
						upright = "Upright";
					}
					graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Multi> ("Things/Pawn/Humanlike/Children/Bodies/Toddler" + upright, ShaderDatabase.CutoutSkin, Vector2.one, graphics.pawn.story.SkinColor);
				}
				// The pawn is a child
				else if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Child) {
					graphics.nakedGraphic = Children_Drawing.GetChildBodyGraphics (graphics, ShaderDatabase.CutoutSkin, graphics.pawn.story.SkinColor);
					graphics.headGraphic = Children_Drawing.GetChildHeadGraphics (ShaderDatabase.CutoutSkin, graphics.pawn.story.SkinColor);
				}
			});
		}

		// My own methods
		internal static Graphic GetChildHeadGraphics(Shader shader, Color skinColor)
		{
			string str = "Male_Child";
			string path = "Things/Pawn/Humanlike/Children/Heads/" + str;
			return GraphicDatabase.Get<Graphic_Multi> (path, shader, Vector2.one, skinColor);
		}
		internal static Graphic GetChildBodyGraphics(PawnGraphicSet graphicSet, Shader shader, Color skinColor)
		{
			string str = "Naked_Boy";
			if (graphicSet.pawn.gender == Gender.Female) {
				str = "Naked_Girl";
			}
			string path = "Things/Pawn/Humanlike/Children/Bodies/" + str;
			return GraphicDatabase.Get<Graphic_Multi> (path, shader, Vector2.one, skinColor);
		}

		// Injected methods
		public static BodyType ModifyChildBodyType(Pawn pawn){
			if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child)
				return BodyType.Thin;
			return pawn.story.bodyType;
		}
		public static Vector3 ModifyChildYPosOffset(Vector3 pos, Pawn pawn, bool portrait){
			Vector3 newPos = pos;
			if (pawn.RaceProps != null && pawn.RaceProps.Humanlike) {
				if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child) {
					newPos.z -= 0.15f;
				}
				if (pawn.ageTracker != null && pawn.ageTracker.CurLifeStageIndex < AgeStage.Child && pawn.InBed () && !portrait) {
					Building_Bed building_Bed = pawn.CurrentBed ();
					if (building_Bed != null) {
						Vector3 vector = new Vector3 (0, 0, 0.5f).RotatedBy (building_Bed.Rotation.AsAngle);
						newPos -= vector;
					}
				}
			}
			return newPos;
		}
		public static Material ModifyHatForChild(Material mat, Pawn pawn){
			if (mat == null)
				return null;
			Material newMat = mat;
			if (pawn.ageTracker.CurLifeStageIndex == 2) {
				newMat.mainTexture.wrapMode = TextureWrapMode.Clamp;
				newMat.mainTextureOffset = new Vector2 (0, 0.018f);
			}
			return newMat;
		}
		public static Material ModifyHairForChild(Material mat, Pawn pawn){
			Material newMat = mat;
			newMat.mainTexture.wrapMode = TextureWrapMode.Clamp;
			// Scale down the child hair to fit the head
			if (pawn.ageTracker.CurLifeStageIndex <= AgeStage.Child) {
				newMat.mainTextureScale = new Vector2 (1.13f, 1.13f);
				newMat.mainTextureOffset = new Vector2 (-0.065f, -0.045f);
			}
			// Scale down the toddler hair to fit the head
			if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Toddler) {
				newMat.mainTextureOffset = new Vector2 (-0.07f, 0.12f);
			}
			return newMat;
		}
		public static Material ModifyClothingForChild(Material damagedMat, Pawn pawn, Rot4 bodyFacing){
			Material newDamagedMat = damagedMat;
			if (pawn.ageTracker.CurLifeStageIndex == 2 && pawn.RaceProps.Humanlike) {
				newDamagedMat.mainTextureScale = new Vector2 (1, 1.3f);
				newDamagedMat.mainTextureOffset = new Vector2 (0, -0.2f);
				if(bodyFacing == Rot4.West || bodyFacing == Rot4.East){
					newDamagedMat.mainTextureOffset = new Vector2 (-0.015f, -0.2f);
				}
			}
			return newDamagedMat;
		}
	}
}

