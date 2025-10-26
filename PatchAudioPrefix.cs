using System;
using System.Collections.Generic;
using System.Text;
using Duckov;
using HarmonyLib;

namespace BFKillFeedback
{
	[HarmonyPatch(typeof(AudioManager), "Post", new Type[] { typeof(string) })]
	public class PatchAudioPost
	{
		static void Prefix(ref string eventName)
		{
			if (ModBehaviour.disable_vanilla_kill_feedback_sound)
			{
				if (eventName == "SFX/Combat/Marker/killmarker_head" || eventName == "SFX/Combat/Marker/killmarker")
				{
					eventName = "";
					return;
				}
			}
		}
	}
}
