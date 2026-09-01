using System.Reflection.Emit;
using Verse.AI;

namespace ServiceRush
{
#pragma warning disable IDE0051

	[HarmonyPatch(typeof(JobDriver_DoBill), "MakeNewToils", MethodType.Enumerator)]
	[HarmonyDebug]
	public static class JobDriver_DoBill_MakeNewToils_Transpiler
	{
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> StopRequiringToTouchTheTable(IEnumerable<CodeInstruction> codeInstructions, ILGenerator ilg)
		{
			CodeMatcher codeMatcher = new(codeInstructions, ilg);

			var instructionsToMatch = new CodeMatch[]
			{
				new(OpCodes.Ldc_I4_1),
				new(OpCodes.Ldc_I4_4),
				new(OpCodes.Call),
				new(OpCodes.Stfld),
			};

			codeMatcher.Start();
			codeMatcher.MatchStartForward(instructionsToMatch);

			codeMatcher.RemoveInstructions(3);

			/*
			codeMatcher.CreateLabel(out var label);

			codeMatcher.MatchStartBackwards(instructionsToMatch);


			var instructionsToInsert = new CodeInstruction[]
			{
				new(OpCodes.Br, label),
			};

			codeMatcher.Insert(instructionsToInsert);
			*/

			return codeMatcher.InstructionEnumeration();
		}
	}
}
