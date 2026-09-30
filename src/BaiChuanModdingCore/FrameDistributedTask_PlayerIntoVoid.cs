using System.IO;
using System.Reflection;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace BaiChuanModdingCore;

public class FrameDistributedTask_PlayerIntoVoid: IFrameDistributedTask
{
	public const float soundPlayingCooldown = 50f;
	
	public bool Run()
	{
		if (Player.main == null) return true;
		string biomeThisTick = Player.main.GetBiomeString();
		if (biomeThisTick == "void" && biomeLastTick != "void")
		{
			float timeNow = Time.time;
			if (timeNow - timeLastPlayed > soundPlayingCooldown)
			{
				PlaySound();
				timeLastPlayed = timeNow;
			}
		}
		biomeLastTick = biomeThisTick;
		return true;
	}

	public static void PlaySound()
	{
		Bus bus = RuntimeManager.GetBus("bus:/master/nofilter/music");
		RESULT result = bus.getChannelGroup(out ChannelGroup channelGroup);
		if (result != RESULT.OK) return;
		RuntimeManager.LowlevelSystem.playSound(sound, channelGroup, false, out channel);
	}
	
	public static bool LoadSound()
	{
		string? dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (dirPath == null)
		{
			BaiChuanModdingCore.logger?.LogWarning("Could not get assembly directory.");
			return false;
		}
		string filePath = Path.Combine(dirPath, "SoundOnPlayerIntoVoid");
		if (!File.Exists(filePath))
		{
			BaiChuanModdingCore.logger?.LogWarning("File SoundOnPlayerIntoVoid doesn't exist: " + filePath);
			return false;
		}
		RESULT result = RuntimeManager.LowlevelSystem.createSound(filePath, MODE._2D | MODE.LOOP_OFF, out Sound newSound);
		if (result != RESULT.OK)
		{
			BaiChuanModdingCore.logger?.LogWarning("Failed to create sound: " + filePath);
			return false;
		}
		sound = newSound;
		BaiChuanModdingCore.logger?.LogDebug("Loaded mod music: " + filePath);
		return true;
	}
	
	public string biomeLastTick = "";

	public float timeLastPlayed;

	public static Sound sound; //非托管资源，但是我懒得释放，直接让游戏被退出时自己清理
	
	public static Channel channel;
}