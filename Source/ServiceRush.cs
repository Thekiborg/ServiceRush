global using HarmonyLib;
global using RimWorld;
global using System;
global using System.Collections.Generic;
global using UnityEngine;
global using Verse;

namespace ServiceRush
{
	[StaticConstructorOnStartup]
	public static class ServiceRush
	{
		static ServiceRush()
		{
			Harmony harmony = new("Thekiborg.ServiceRush");
			harmony.PatchAll();
		}
	}
}
