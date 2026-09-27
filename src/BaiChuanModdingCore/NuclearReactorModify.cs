using System.Collections.Generic;
using System.Reflection;

namespace BaiChuanModdingCore;

public class NuclearReactorModify
{
	public static void DoModify()
	{
		object? charge = typeof(BaseNuclearReactor).GetField("charge", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
		if (charge is Dictionary<TechType, float> dictionary)
		{
			dictionary[TechType.ReactorRod] = 60000f;
			return;
		}
		BaiChuanModdingCore.logger?.LogError("Failed to modify the charge of ReactorRod.");
	}
}