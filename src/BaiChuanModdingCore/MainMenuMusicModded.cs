using System.IO;
using System.Reflection;
using FMOD;
using FMOD.Studio;
using FMODUnity;

namespace BaiChuanModdingCore
{
	public class MainMenuMusicModded
	{
		public static void PlayPostfix(MainMenuMusic __instance)
		{
			modMusicChannel?.stop();
			if (modMusic != null)
			{
				Bus bus = RuntimeManager.GetBus("bus:/master/nofilter/music");
				RESULT result = bus.getChannelGroup(out ChannelGroup channelGroup);
				if (result != RESULT.OK) return;
				result = RuntimeManager.LowlevelSystem.playSound((Sound)modMusic, channelGroup, false, out Channel channel);
				if (result != RESULT.OK) return;
				modMusicChannel = channel;
			}
		}

		public static void StopPostfix(MainMenuMusic __instance)
		{
			modMusicChannel?.stop();
			modMusicChannel = null;
		}

		public static bool LoadModMusic()
		{
			string? dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (dirPath == null)
			{
				BaiChuanModdingCore.logger?.LogWarning("Could not get assembly directory.");
				return false;
			}
			string filePath = Path.Combine(dirPath, "MainMenuMusic");
			if (!File.Exists(filePath))
			{
				BaiChuanModdingCore.logger?.LogWarning("File MainMenuMusic doesn't exist: " + filePath);
				return false;
			}
			RESULT result = RuntimeManager.LowlevelSystem.createSound(filePath, MODE._2D, out Sound newSound);
			if (result != RESULT.OK)
			{
				BaiChuanModdingCore.logger?.LogWarning("Failed to create sound: " + filePath);
				return false;
			}
			modMusic = newSound;
			BaiChuanModdingCore.logger?.LogDebug("Loaded mod music: " + filePath);
			return true;
		}
		
		public static Sound? modMusic;

		public static Channel? modMusicChannel;
	}
}