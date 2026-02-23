using BepInEx;
using BepInEx.Logging;

namespace BaiChuanCustomCore
{
	[BepInPlugin("net.bcasoft.baichuan_modding_core", "BaiChuanModdingCore", "0.0.1.0")]
	public class BaiChuanModdingCore : BaseUnityPlugin
	{
		private void Awake()
		{
			Log = Logger;
		}
		
		internal static ManualLogSource Log;
	}
}