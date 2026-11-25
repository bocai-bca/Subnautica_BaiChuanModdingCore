using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BaiChuanModdingCore.Features.TooltipRainbowGradient
{
	[HarmonyPatch(typeof(Text), "OnPopulateMesh", new[] { typeof(VertexHelper) })]
	internal static class TooltipRainbowGradientPatch
	{
		private static readonly FieldInfo? FieldTooltipMain =
			typeof(uGUI_Tooltip).GetField("main", BindingFlags.NonPublic | BindingFlags.Static);
		private static readonly FieldInfo? FieldTooltipText =
			typeof(uGUI_Tooltip).GetField("text", BindingFlags.Public | BindingFlags.Instance);
		private static readonly FieldInfo? FieldVisible =
			typeof(uGUI_Tooltip).GetField("visible", BindingFlags.NonPublic | BindingFlags.Static);

		private static Text? _cachedTooltipText;
		private static string? _cachedKeywordSource;
		private static List<string> _keywords = new List<string>();

		private static float _hueOffset;
		private static bool _isTarget;
		private static int _frame;

		internal static void Tick()
		{
			if (!TooltipRainbowGradientFeature.CfgEnabled.Value)
			{
				return;
			}
			if (!TooltipRainbowGradientFeature.CfgAnimated.Value)
			{
				return;
			}
			if (!_isTarget)
			{
				return;
			}

			Text? text = ResolveTooltipText();
			if (text == null)
			{
				return;
			}
			if (!IsVisible())
			{
				_frame = 0;
				return;
			}

			int interval = TooltipRainbowGradientFeature.CfgInterval.Value;
			if (interval < 1)
			{
				interval = 1;
			}
			_frame++;
			if (_frame < interval)
			{
				return;
			}
			_frame = 0;

			float dt = Time.unscaledDeltaTime * interval;
			_hueOffset += TooltipRainbowGradientFeature.CfgFlowSpeed.Value * dt;
			_hueOffset -= Mathf.Floor(_hueOffset);

			// 触发该 Text 重新生成网格，后置补丁会用新的色相偏移重涂。
			text.SetVerticesDirty();
		}

		private static bool IsVisible()
		{
			try
			{
				object? value = FieldVisible != null ? FieldVisible.GetValue(null) : null;
				return value is bool visible && visible;
			}
			catch
			{
				return false;
			}
		}

		[HarmonyPostfix]
		private static void Postfix(Text __instance, VertexHelper toFill)
		{
			if (!TooltipRainbowGradientFeature.CfgEnabled.Value)
			{
				return;
			}
			if (toFill == null)
			{
				return;
			}

			Text? target = ResolveTooltipText();
			if (target == null || __instance != target)
			{
				return;
			}

			string raw = __instance.text;
			if (string.IsNullOrEmpty(raw))
			{
				return;
			}

			int newline = raw.IndexOf('\n');
			string first = newline >= 0 ? raw.Substring(0, newline) : raw;
			first = StripTags(first).Trim();
			if (first.Length == 0)
			{
				_isTarget = false;
				return;
			}

			if (!MatchesKeyword(first))
			{
				_isTarget = false;
				return;
			}
			_isTarget = true;

			int vertexCount = toFill.currentVertCount;
			if (vertexCount <= 0)
			{
				return;
			}

			float lineHeight;
			TextGenerator generator = __instance.cachedTextGenerator;
			if (generator != null && generator.lines != null &&
				generator.lines.Count > 0 && generator.lines[0].height > 0)
			{
				lineHeight = generator.lines[0].height;
			}
			else
			{
				lineHeight = __instance.fontSize * __instance.lineSpacing;
			}

			UIVertex vertex = default;
			float maxY = float.MinValue;
			for (int i = 0; i < vertexCount; i++)
			{
				toFill.PopulateUIVertex(ref vertex, i);
				if (vertex.position.y > maxY)
				{
					maxY = vertex.position.y;
				}
			}

			float bandTop = maxY + 1f;
			float bandBottom = maxY - lineHeight * 1.05f;

			float minX = float.MaxValue;
			float maxX = float.MinValue;
			for (int i = 0; i < vertexCount; i++)
			{
				toFill.PopulateUIVertex(ref vertex, i);
				if (vertex.position.y > bandTop || vertex.position.y < bandBottom)
				{
					continue;
				}
				if (vertex.position.x < minX)
				{
					minX = vertex.position.x;
				}
				if (vertex.position.x > maxX)
				{
					maxX = vertex.position.x;
				}
			}

			float span = maxX - minX;
			if (span <= 0.0001f)
			{
				return;
			}

			float saturation = TooltipRainbowGradientFeature.CfgSaturation.Value;
			float brightness = TooltipRainbowGradientFeature.CfgBrightness.Value;
			float hueSpan = TooltipRainbowGradientFeature.CfgHueSpan.Value;
			float hueBase = TooltipRainbowGradientFeature.CfgHueOffset.Value + _hueOffset;

			for (int i = 0; i < vertexCount; i++)
			{
				toFill.PopulateUIVertex(ref vertex, i);
				if (vertex.position.y > bandTop || vertex.position.y < bandBottom)
				{
					continue;
				}

				float t = (vertex.position.x - minX) / span;
				if (t < 0f)
				{
					t = 0f;
				}
				if (t > 1f)
				{
					t = 1f;
				}

				float hue = hueBase + t * hueSpan;
				hue -= Mathf.Floor(hue);

				Color color = Color.HSVToRGB(hue, saturation, brightness);
				vertex.color = new Color32(
					(byte)(Mathf.Clamp01(color.r) * 255f),
					(byte)(Mathf.Clamp01(color.g) * 255f),
					(byte)(Mathf.Clamp01(color.b) * 255f),
					vertex.color.a);

				toFill.SetUIVertex(vertex, i);
			}
		}

		private static Text? ResolveTooltipText()
		{
			if (_cachedTooltipText != null)
			{
				return _cachedTooltipText;
			}

			try
			{
				object? main = FieldTooltipMain != null ? FieldTooltipMain.GetValue(null) : null;
				if (main == null)
				{
					return null;
				}

				object? text = FieldTooltipText != null ? FieldTooltipText.GetValue(main) : null;
				_cachedTooltipText = text as Text;
				return _cachedTooltipText;
			}
			catch
			{
				return null;
			}
		}

		private static bool MatchesKeyword(string title)
		{
			if (_cachedKeywordSource != TooltipRainbowGradientFeature.CfgKeywords.Value)
			{
				_cachedKeywordSource = TooltipRainbowGradientFeature.CfgKeywords.Value;
				_keywords = new List<string>();
				if (!string.IsNullOrEmpty(_cachedKeywordSource))
				{
					string[] parts = _cachedKeywordSource.Split(new[] { ',', '，', ';', '；' });
					for (int i = 0; i < parts.Length; i++)
					{
						string part = parts[i].Trim();
						if (part.Length > 0)
						{
							_keywords.Add(part);
						}
					}
				}
			}

			for (int i = 0; i < _keywords.Count; i++)
			{
				if (title.Contains(_keywords[i]))
				{
					return true;
				}
			}

			return false;
		}

		private static string StripTags(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			var builder = new System.Text.StringBuilder(value.Length);
			bool inTag = false;
			for (int i = 0; i < value.Length; i++)
			{
				char ch = value[i];
				if (ch == '<')
				{
					inTag = true;
					continue;
				}
				if (ch == '>')
				{
					inTag = false;
					continue;
				}
				if (!inTag)
				{
					builder.Append(ch);
				}
			}

			return builder.ToString();
		}
	}
}
