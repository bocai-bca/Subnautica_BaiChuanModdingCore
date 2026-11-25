using BepInEx.Configuration;
using BepInEx.Logging;

namespace BaiChuanModdingCore.Features.TooltipRainbowGradient
{
	/// <summary>
	/// 独立功能：只给符合关键字条件的提示框标题上彩虹渐变。
	/// 配置、状态和 Harmony 补丁都放在本文件夹内，不与其他功能混写。
	/// </summary>
	internal static class TooltipRainbowGradientFeature
	{
		internal static ConfigEntry<bool> CfgEnabled = null!;
		internal static ConfigEntry<string> CfgKeywords = null!;
		internal static ConfigEntry<bool> CfgAnimated = null!;
		internal static ConfigEntry<float> CfgFlowSpeed = null!;
		internal static ConfigEntry<int> CfgInterval = null!;
		internal static ConfigEntry<float> CfgSaturation = null!;
		internal static ConfigEntry<float> CfgBrightness = null!;
		internal static ConfigEntry<float> CfgHueSpan = null!;
		internal static ConfigEntry<float> CfgHueOffset = null!;

		internal static ManualLogSource? Log;

		internal static void Initialize(ConfigFile config, ManualLogSource log)
		{
			Log = log;
			CfgEnabled = config.Bind(
				"TooltipRainbowGradient.General",
				"Enabled",
				true,
				"总开关。关闭后提示框标题恢复原色。");
			CfgKeywords = config.Bind(
				"TooltipRainbowGradient.General",
				"Keywords",
				"寰宇,无尽",
				"标题关键字，逗号分隔。标题（第一行）包含任一关键字时才上渐变。");

			CfgAnimated = config.Bind(
				"TooltipRainbowGradient.Animation",
				"Animated",
				true,
				"是否让彩虹流动。关闭则静态渐变。");
			CfgFlowSpeed = config.Bind(
				"TooltipRainbowGradient.Animation",
				"FlowSpeed",
				0.12f,
				new ConfigDescription(
					"流动速度：每秒推进多少个完整色环。0.12 约 8 秒走一圈。",
					new AcceptableValueRange<float>(0f, 2f)));
			CfgInterval = config.Bind(
				"TooltipRainbowGradient.Animation",
				"RebuildIntervalFrames",
				2,
				new ConfigDescription(
					"每隔几帧刷新一次渐变。1=每帧（最顺滑但最费），2~3 肉眼基本无差别。",
					new AcceptableValueRange<int>(1, 10)));

			CfgSaturation = config.Bind(
				"TooltipRainbowGradient.Style",
				"Saturation",
				0.90f,
				new ConfigDescription(
					"饱和度 0~1",
					new AcceptableValueRange<float>(0f, 1f)));
			CfgBrightness = config.Bind(
				"TooltipRainbowGradient.Style",
				"Brightness",
				1.00f,
				new ConfigDescription(
					"亮度 0~1",
					new AcceptableValueRange<float>(0f, 1f)));
			CfgHueSpan = config.Bind(
				"TooltipRainbowGradient.Style",
				"HueSpan",
				0.85f,
				new ConfigDescription(
					"色相跨度，1 表示绕满整圈（真彩虹）",
					new AcceptableValueRange<float>(0f, 1f)));
			CfgHueOffset = config.Bind(
				"TooltipRainbowGradient.Style",
				"HueOffset",
				0.00f,
				new ConfigDescription(
					"起始色相偏移，0=红 0.33=绿 0.66=蓝",
					new AcceptableValueRange<float>(0f, 1f)));

			Log?.LogInfo("TooltipRainbowGradient 功能已初始化。");
		}

		internal static void Tick()
		{
			TooltipRainbowGradientPatch.Tick();
		}
	}
}
