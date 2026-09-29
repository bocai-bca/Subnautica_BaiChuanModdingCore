namespace BaiChuanModdingCore;

/// <summary>
/// 帧分布式任务接口
/// </summary>
public interface IFrameDistributedTask
{
	/// <summary>
	/// 执行该帧分布式任务。
	/// </summary>
	/// <returns>该任务是否完成并将指针跳转到下一个任务，如果为<c>false</c>，则下一帧将继续调用本实例。适用于若本任务在当前帧中过于繁重，可以告知管理器分散在多个帧内。</returns>
	public bool Run();
}