using System;
using System.IO;
using System.Reflection;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaiChuanModdingCore;

public class FrameDistributedTask_GargantuanMusic: IFrameDistributedTask
{
	/// <summary>
	/// 状态
	/// </summary>
	public enum State
	{
		/// <summary>
		/// 未开始
		/// </summary>
		NOT_STARTED,
		/// <summary>
		/// 正在播放入场
		/// </summary>
		ENTERING,
		/// <summary>
		/// 正在播放中间循环
		/// </summary>
		LOOPING,
		/// <summary>
		/// 正在播放出场
		/// </summary>
		EXITING,
	}
	
	public bool Run()
	{
		switch (state)
		{
			case State.NOT_STARTED:
				if (FindGargantuan() && gargantuanGameObject is { activeSelf: true })
				{
					PlaySoundEntering();
					state = State.ENTERING;
				}
				break;
			case State.ENTERING:
				break;
			case State.LOOPING:
				if (gargantuanGameObject is null || !gargantuanGameObject.activeSelf)
				{
					PlaySoundExiting();
					state = State.EXITING;
				}
				break;
			case State.EXITING:
				channelExiting.isPlaying(out bool isplaying);
				if (!isplaying) state = State.NOT_STARTED;
				break;
			default:
				BaiChuanModdingCore.logger?.LogError("FrameDistributedTask_GargantuanMusic state out of range.");
				break;
		}
		return true;
	}

	public static bool FindGargantuan()
	{
		if (gargantuanGameObject is not null) return true;
		Scene sceneMain = SceneManager.GetSceneByName("Main");
		if (!sceneMain.IsValid() || !sceneMain.isLoaded)
		{
			BaiChuanModdingCore.logger?.LogError("Could not get scene Main.");
			return false;
		}
		foreach (GameObject gameObject in sceneMain.GetRootGameObjects())
		{
			if (gameObject.name != "Landscape") continue;
			if (gameObject.transform.Find("Global Root/GargantuanVoid(Clone)") is not { } transform) continue;
			gargantuanGameObject = transform.gameObject;
			return true;
		}
		BaiChuanModdingCore.logger?.LogError("Could not get GameObject of GargantuanVoid.");
		return false;
	}
	
	public static void PlaySoundEntering()
	{
		try
		{
			FrameDistributedTask_PlayerIntoVoid.channel.stop();
		}
		catch (Exception e)
		{
			BaiChuanModdingCore.logger?.LogError("Exception on trying to stop sound channel of PlayerIntoVoid. E: " + e.Message);
		}
		Bus bus = RuntimeManager.GetBus("bus:/master/nofilter/music");
		RESULT result = bus.getChannelGroup(out ChannelGroup channelGroup);
		if (result != RESULT.OK) return;
		RuntimeManager.LowlevelSystem.playSound(soundEntering, channelGroup, false, out channelEntering);
		channelEntering.setCallback(OnSoundEnd);
	}

	public static void PlaySoundLooping()
	{
		try
		{
			channelEntering.stop();
		}
		catch (Exception e)
		{
			BaiChuanModdingCore.logger?.LogError("Exception on trying to stop sound channel which is GargantuanMusic.channelEntering. E: " + e.Message);
		}
		state = State.LOOPING;
		Bus bus = RuntimeManager.GetBus("bus:/master/nofilter/music");
		RESULT result = bus.getChannelGroup(out ChannelGroup channelGroup);
		if (result != RESULT.OK) return;
		RuntimeManager.LowlevelSystem.playSound(soundLooping, channelGroup, false, out channelLooping);
	}

	public static void PlaySoundExiting()
	{
		if (channelLooping.isPlaying(out bool isplaying) == RESULT.OK)
		{
			if (isplaying) channelLooping.stop();
		}
		state = State.EXITING;
		Bus bus = RuntimeManager.GetBus("bus:/master/nofilter/music");
		RESULT result = bus.getChannelGroup(out ChannelGroup channelGroup);
		if (result != RESULT.OK) return;
		RuntimeManager.LowlevelSystem.playSound(soundExiting, channelGroup, false, out channelExiting);
	}
	
	public static bool LoadSound()
	{
		string? dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (dirPath == null)
		{
			BaiChuanModdingCore.logger?.LogWarning("Could not get assembly directory.");
			return false;
		}
		string filePath = Path.Combine(dirPath, "SoundGargantuanEntering");
		if (!File.Exists(filePath))
		{
			BaiChuanModdingCore.logger?.LogWarning("File SoundGargantuanEntering doesn't exist: " + filePath);
			return false;
		}
		RESULT result = RuntimeManager.LowlevelSystem.createSound(filePath, MODE._2D | MODE.LOOP_OFF, out Sound newSound);
		if (result != RESULT.OK)
		{
			BaiChuanModdingCore.logger?.LogWarning("Failed to create sound: " + filePath);
			return false;
		}
		soundEntering = newSound;
		filePath = Path.Combine(dirPath, "SoundGargantuanLooping");
		if (!File.Exists(filePath))
		{
			BaiChuanModdingCore.logger?.LogWarning("File SoundGargantuanLooping doesn't exist: " + filePath);
			return false;
		}
		result = RuntimeManager.LowlevelSystem.createSound(filePath, MODE._2D | MODE.LOOP_NORMAL, out newSound);
		if (result != RESULT.OK)
		{
			BaiChuanModdingCore.logger?.LogWarning("Failed to create sound: " + filePath);
			return false;
		}
		soundLooping = newSound;
		filePath = Path.Combine(dirPath, "SoundGargantuanExiting");
		if (!File.Exists(filePath))
		{
			BaiChuanModdingCore.logger?.LogWarning("File SoundGargantuanExiting doesn't exist: " + filePath);
			return false;
		}
		result = RuntimeManager.LowlevelSystem.createSound(filePath, MODE._2D | MODE.LOOP_OFF, out newSound);
		if (result != RESULT.OK)
		{
			BaiChuanModdingCore.logger?.LogWarning("Failed to create sound: " + filePath);
			return false;
		}
		soundExiting = newSound;
		BaiChuanModdingCore.logger?.LogDebug("Loaded mod music: " + filePath);
		return true;
	}
	
	public static Sound soundEntering;
	public static Sound soundLooping;
	public static Sound soundExiting;
	
	public static Channel channelEntering;
	public static Channel channelLooping;
	public static Channel channelExiting;
	
	public static State state = State.NOT_STARTED;

	public static GameObject? gargantuanGameObject;
	
	// 本方法由Deepseek LLM提供
	[AOT.MonoPInvokeCallback(typeof(CHANNEL_CALLBACK))]
	private static RESULT OnSoundEnd(nint channelraw, CHANNELCONTROL_TYPE controltype, CHANNELCONTROL_CALLBACK_TYPE type, nint commanddata1, nint commanddata2)
	{
		if (type == CHANNELCONTROL_CALLBACK_TYPE.END)
		{
			// 声音自然播放结束
			// 注意：可能不在主线程，只设标志位，回主线程再处理 Unity 逻辑
			PlaySoundLooping();
		}
		return RESULT.OK;
	}
}