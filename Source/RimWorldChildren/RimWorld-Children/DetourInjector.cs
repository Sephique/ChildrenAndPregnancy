using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RimWorldChildren
{
	[StaticConstructorOnStartup]
	internal static class DetourInjector
	{
		//private static Assembly Assembly => Assembly.GetAssembly(typeof(DetourInjector));

		//private static string AssemblyName => Assembly.FullName.Split(',').First();
		private static string AssemblyName = Assembly.GetAssembly(typeof(DetourInjector)).FullName.Split(',').First();

		static DetourInjector()
		{
			LongEventHandler.QueueLongEvent(Inject, "Initializing", true, null);
		}

		private static void Inject()
		{
			if (DoInject()){
				Log.Message(AssemblyName + " injected.");
				Log.Message("Simple Beard Framework injected.");
			}
			else
				Log.Error(AssemblyName + " failed to get injected properly.");
		}

		private const BindingFlags UniversalBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static bool DoInject()
		{
//			// Detour ResolveAllGraphics method from PawnGraphicSet class
//			// Required to inject body graphics for children
//			MethodInfo Method1A = typeof(Verse.PawnGraphicSet).GetMethod("ResolveAllGraphics");
//			MethodInfo Method1B = typeof(ChildGraphics).GetMethod("_ResolveAllGraphics", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method1A, Method1B)){
//				ErrorDetouring("PawnGraphicsSet.ResolveAllGraphics");
//				return false;
//			}

//			// Detour ResolveApparelGraphics method from PawnGraphicSet class
//			// Required to fit apparel graphics to child
//			MethodInfo Method2A = typeof(Verse.PawnGraphicSet).GetMethod("ResolveApparelGraphics");
//			MethodInfo Method2B = typeof(ChildGraphics).GetMethod("_ResolveApparelGraphics", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method2A, Method2B)) {
//				ErrorDetouring("PawnGraphicsSet.ResolveApparelGraphics");
//				return false;
//			}

//			// Detour RenderPawnInternal method from PawnRenderer class
//			// Required to render children properly
//			// Because there are overloads for RenderPawnInternal, we must specify exactly which one by which arguments it takes
//			MethodInfo Method3A = typeof(Verse.PawnRenderer).GetMethod("RenderPawnInternal", UniversalBindingFlags, null, new Type[] { typeof(Vector3), typeof(Quaternion), typeof(Boolean), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(Boolean), typeof(Boolean)}, null);
//			MethodInfo Method3B = typeof(ChildGraphics).GetMethod("_RenderPawnInternal", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method3A, Method3B)) {
//				ErrorDetouring("PawnRenderer.RenderPawnInternal");
//				return false;
//			}

			// Detour TryGiveJob for OptimizeApparel
			// Required to stop Toddlers from trying to wear clothes
//			MethodInfo Method4A = typeof(JobGiver_OptimizeApparel).GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);
//			MethodInfo Method4B = typeof(ChildCode).GetMethod("_TryGiveJob", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method4A, Method4B)){
//				ErrorDetouring("JobGiver_OptimizeApparel.TryGiveJob");
//				return false;
//			}
//			// Detour MakeNewToils for JobDriver_Wear
//			// Required to completely disable toddlers from wearing clothes
//			MethodInfo Method5A = typeof(RimWorld.JobDriver_Wear).GetMethod("MakeNewToils", UniversalBindingFlags);
//			MethodInfo Method5B = typeof(Wear_Override).GetMethod("_MakeNewToils", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method5A, Method5B)){
//				ErrorDetouring("JobDriver_Wear.MakeNewToils");
//				return false;
//			}

//			// Thought overrides
//			// Required to stop toddlers/babies from getting unreasonable thoughts (eg: I'm nude! boohoo ;_; )
//			MethodInfo Method6A = typeof(ThoughtUtility).GetMethod("CanGetThought", UniversalBindingFlags);
//			MethodInfo Method6B = typeof(Thought_Override).GetMethod("_CanGetThought", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method6A, Method6B)){
//				ErrorDetouring("ThoughtUtility.CanGetThought");
//				return false;
//			}
//			MethodInfo Method7A = typeof(PawnGenerator).GetMethod("GeneratePawn", UniversalBindingFlags, null, new Type[] {typeof(PawnGenerationRequest)}, null);
//			MethodInfo Method7B = typeof(ChildCode).GetMethod("_GeneratePawn", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method7A, Method7B)){
//				ErrorDetouring("PawnGenerator.GeneratePawn");
//				return false;
//			}

			// Gets rid of random death. Needed to stop children from spawning dead.
			// Could configure to allow random death 
//			MethodInfo Method8A = typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange", UniversalBindingFlags);
//			MethodInfo Method8B = typeof(ChildCode).GetMethod("_CheckForStateChange", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method8A, Method8B)){
//				ErrorDetouring("Pawn_HealthTracker.CheckForStateChange");
//				return false;
//			}

//			// Detour MakeNewToils for JobDriver_Lovin
//			// Required to make Lovin actually cause pregnancy
//			MethodInfo Method9A = typeof(JobDriver_Lovin).GetMethod("MakeNewToils", UniversalBindingFlags);
//			MethodInfo Method9B = typeof(Lovin_Override).GetMethod("_MakeNewToils", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method9A, Method9B)){
//				ErrorDetouring("JobDriver_Lovin.MakeNewToils");
//				return false;
//			}
//			MethodInfo RimWorld_JobGiver_DoLovin_TryGiveJob = typeof(RimWorld.JobGiver_DoLovin).GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);
//			MethodInfo Children_JobGiver_DoLovin_TryGiveJob = typeof(Lovin_Override).GetMethod("_TryGiveJob", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(RimWorld_JobGiver_DoLovin_TryGiveJob, Children_JobGiver_DoLovin_TryGiveJob)){
//				ErrorDetouring("JobGiver_DoLovin.TryGiveJob");
//				return false;
//			}

//			MethodInfo Method10A = typeof(Verse.Pawn_HealthTracker).GetMethod("ShouldBeDowned", UniversalBindingFlags);
//			MethodInfo Method10B = typeof(ChildCode).GetMethod("_ShouldBeDowned", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method10A, Method10B)){
//				ErrorDetouring("Pawn_HealthTracker.ShouldBeDowned");
//				return false;
//			}

//			MethodInfo Method11A = typeof(Pawn_ApparelTracker).GetMethod("ApparelChanged", UniversalBindingFlags);
//			MethodInfo Method11B = typeof(ChildGraphics).GetMethod("_ApparelChanged", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method11A, Method11B)) {
//				ErrorDetouring("Pawn_ApparelTracker.ApparelChanged");
//				return false;
//			}

//			MethodInfo Method12A = typeof(JobGiver_SocialFighting).GetMethod("TryGiveJob", UniversalBindingFlags);
//			MethodInfo Method12B = typeof(Thought_Override).GetMethod("_TryGiveJob_SocialFight", UniversalBindingFlags);
//			if (!Detours.TryDetourFromTo(Method12A, Method12B)) {
//				ErrorDetouring("JobGiver_SocialFighting.TryGiveJob");
//				return false;
//			}

			// Mod incompatibility checking
			foreach (ModMetaData mod in ModLister.AllInstalledMods) {
				// List of all known incompatible mods
				List<string> incompatible_mods = new List<string> {
					"Humanoid Alien Races",
					"Facial Stuff",
					"A World Without Hat"
				};

				if(mod.Active){
					// Check through the list
					foreach (String incapmod in incompatible_mods) {
						if (mod.Name.Contains(incapmod)) {
								ErrorIncompatibleMod (mod);
						}
					}
					// Special cases
					if(mod.Name == "Simple Beard Framework"){
						Log.Error(AssemblyName + ": Simple Beard Framework is integrated in this mod, do not use the standalone version with " + AssemblyName);
					}
				}
			}

			return true;
		}

		internal static void ErrorDetouring(string classmethod){
			Log.Error(AssemblyName + ": Failed to inject " + classmethod + " detour!");
		}

		internal static void ErrorIncompatibleMod(ModMetaData othermod){
			string name = AssemblyName;
			string modname =  othermod.Name;
			if (Prefs.DevMode)
				Log.Error ("Error initializing " + name + ": incompatibilty found with mod " + modname + ". Disable either " + modname + " or " + name + ".");
			else
				Messages.Message ("Error initializing " + name + ": incompatibilty found with mod " + modname + ". Disable either " + modname + " or " + name + ".", MessageSound.Negative);
		}
	}
}