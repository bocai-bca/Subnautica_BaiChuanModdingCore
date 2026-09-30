using System.Collections.Generic;

namespace BaiChuanModdingCore;

/// <summary>
/// 帧分布式任务管理器，类似于协程的设计思路。将多种不同的低需求任务分摊到多个帧，以减轻长时间内的性能负担，并尽力避免low帧发生。
/// </summary>
public static class FrameDistributedTaskManager
{
	/// <summary>
	/// 指针回滚冷却，需要为一个负数。即任务指针达到末尾时回到的位置，设为负数时能够使得指针在负数时不执行任何任务，来迫使长时间内的性能负担减轻。单位为帧。
	/// </summary>
	public const int POINT_ROLLING_BACK_COOLDOWN = -50;
	
	/// <summary>
	/// 任务表
	/// </summary>
	public static readonly List<IFrameDistributedTask> tasks = [
		new FrameDistributedTask_PlayerIntoVoid(),
		new FrameDistributedTask_GargantuanMusic(),
	];

	/// <summary>
	/// 任务指针
	/// </summary>
	public static int taskPoint = 0;
	
	public static void Update()
	{
		if (tasks.Count <= 0)
		{
			taskPoint++;
			return;
		}
		if (taskPoint >= tasks.Count) taskPoint = 0;
		if (tasks[taskPoint].Run()) taskPoint++;
	}
}