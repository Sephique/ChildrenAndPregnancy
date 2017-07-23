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
	[HarmonyBefore(new string[] { "rimworld.erdelf.alien_race.main"})]
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
				new CodeInstruction(OpCodes.Ldarg_S, 7), //portrait
				new CodeInstruction(OpCodes.Call, typeof(Children_Drawing).GetMethod("ModifyChildYPosOffset")),
			};
			ILs.InsertRange (injectIndex0, injection0);
			foreach(int i in new List<int>{5,6,7, 11, 24}){
				ILs.InsertRange (ILs.FindIndex (x => x.opcode == OpCodes.Stloc_S && x.operand as LocalBuilder != null && ((LocalBuilder)x.operand).LocalIndex == i), injection0);
			}

			int injectIndex1 = ILs.FindIndex (x => x.opcode == OpCodes.Ldarg_3);
			Label babyDrawBodyJump = ILgen.DefineLabel ();
			ILs [injectIndex1 + 2].labels = new List<Label>{ babyDrawBodyJump };
			List<CodeInstruction> injection1 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderer), "pawn")),
				new CodeInstruction (OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "ageTracker")),
				new CodeInstruction (OpCodes.Call, typeof(Pawn_AgeTracker).GetProperty("CurLifeStageIndex").GetGetMethod()),
				new CodeInstruction (OpCodes.Ldc_I4_2),
				new CodeInstruction (OpCodes.Blt, babyDrawBodyJump),
			};
			ILs.InsertRange (injectIndex1, injection1);

			// Ensure pawn is a child or higher before drawing head
			int injectIndex2 = ILs.FindIndex (x => x.opcode == OpCodes.Ldfld && x.operand == AccessTools.Field (typeof(PawnGraphicSet), "headGraphic")) + 2;
			List<CodeInstruction> injection2 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
				new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod("EnsurePawnIsChildOrOlder")),
				new CodeInstruction (OpCodes.Brfalse, ILs [injectIndex2 - 1].operand),
			};
			ILs.InsertRange (injectIndex2, injection2);

			// Modify the scale of a hat graphic when worn by a child
			int injectIndex3 = ILs.GetRange(injectIndex2, ILs.Count - injectIndex2).FindIndex (x => x.opcode == OpCodes.Stloc_S && x.operand is LocalBuilder && ((LocalBuilder)x.operand).LocalIndex == 16) + 1;
			List<CodeInstruction> injection3 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldloc_S, 16),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
				new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod("ModifyHatForChild")),
				new CodeInstruction (OpCodes.Stloc_S, 16),
			};
			ILs.InsertRange (injectIndex3, injection3);

			// Modify the scale of a hair graphic when drawn on a child
			int injectIndex4 = ILs.FindIndex (x => x.opcode == OpCodes.Callvirt && x.operand == AccessTools.Method (typeof(PawnGraphicSet), "HairMatAt")) + 2;
			List<CodeInstruction> injection4 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldloc_S, 18),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
				new CodeInstruction (OpCodes.Call, AccessTools.Method(typeof(Children_Drawing), "ModifyHairForChild")),
				new CodeInstruction (OpCodes.Stloc_S, 18),
			};
			ILs.InsertRange (injectIndex4, injection4);

			// Modify the scale of clothing graphics when worn by a child
			int injectIndex5 = ILs.FindIndex (x => x.opcode == OpCodes.Stloc_S && x.operand is LocalBuilder && ((LocalBuilder)x.operand).LocalIndex == 4) + 1;
			List<CodeInstruction> injection5 = new List<CodeInstruction> {
				new CodeInstruction (OpCodes.Ldloc_S, 4),
				new CodeInstruction (OpCodes.Ldarg_0),
				new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField ("pawn", AccessTools.all)),
				new CodeInstruction (OpCodes.Ldarg_S, 4),
				new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod ("ModifyClothingForChild")),
				new CodeInstruction (OpCodes.Stloc_S, 4),
			};
			ILs.InsertRange (injectIndex5, injection5);

			foreach (CodeInstruction IL in ILs) {
				yield return IL;
			}
		}
	}

	internal static class Children_Drawing
	{
		internal static void ResolveAgeGraphics(PawnGraphicSet graphics){
			LongEventHandler.ExecuteWhenFinished (delegate {

				//if (!graphics.pawn.RaceProps.Humanlike) {
				if (graphics.pawn.def.defName != "Human") {
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

					// The pawn is a baby
					if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Baby) {
						graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Single> ("Things/Pawn/Humanlike/Children/Bodies/Newborn", ShaderDatabase.CutoutSkin, Vector2.one, graphics.pawn.story.SkinColor);
					}
				}

				// The pawn is a toddler
				if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Toddler) {
					string upright = "";
					if (graphics.pawn.ageTracker.AgeBiologicalYears >= 1) {
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
				if (pawn.InBed () && !portrait) {
					Building_Bed bed = pawn.CurrentBed ();
					if (pawn.ageTracker.CurLifeStageIndex < AgeStage.Child) {
						Vector3 vector = new Vector3 (0, 0, 0.5f).RotatedBy (bed.Rotation.AsAngle);
						newPos -= vector;
					} else if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child && bed.def.size.z == 1) { // Are we in a crib?
						Vector3 vector = new Vector3 (0, 0, 0.5f).RotatedBy (bed.Rotation.AsAngle);
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
		public static bool EnsurePawnIsChildOrOlder(Pawn pawn){
			if(pawn.ageTracker.CurLifeStageIndex >= AgeStage.Child)
				return true;
			return false;
		}
		public static Material ModifyHairForChild(Material mat, Pawn pawn){
			Material newMat = mat;
			newMat.mainTexture.wrapMode = TextureWrapMode.Clamp;
			// Scale down the child hair to fit the head
			if (pawn.ageTracker.CurLifeStageIndex <= AgeStage.Child) {
				newMat.mainTextureScale = new Vector2 (1.13f, 1.13f);
				float benis = 0;
				if (!pawn.Rotation.IsHorizontal) {
					benis = -0.015f;
				}
				newMat.mainTextureOffset = new Vector2 (-0.045f + benis, -0.045f);
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

