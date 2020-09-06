using System;
using System.Threading;
using UnityEngine;

namespace ETModel
{
	public class Init : MonoBehaviour
	{
		private void Start()
		{
			this.StartAsync().Coroutine();
		}
		
		private async ETVoid StartAsync()
		{
			try
			{
				SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);

				DontDestroyOnLoad(gameObject);
				ClientConfigHelper.SetConfigHelper();
				Game.EventSystem.Add(DLLType.Core, typeof(Core).Assembly);
				Game.EventSystem.Add(DLLType.Model, typeof(Init).Assembly);

				Game.Scene.AddComponent<GlobalConfigComponent>();
				Game.Scene.AddComponent<ResourcesComponent>();
				await BundleHelper.DownloadBundle();
				Game.Scene.GetComponent<ResourcesComponent>().LoadBundle("config.unity3d");
				Game.Scene.AddComponent<ConfigComponent>();
				Game.Scene.GetComponent<ResourcesComponent>().UnloadBundle("config.unity3d");

				UnitConfig unitConfig = (UnitConfig)Game.Scene.GetComponent<ConfigComponent>().Get(typeof(UnitConfig), 1001);
				Log.Debug($"config {JsonHelper.ToJson(unitConfig)}");


				//添加指定与网络组件
				Game.Scene.AddComponent<OpcodeTypeComponent>();
				Game.Scene.AddComponent<NetOuterComponent>();
				Game.Scene.AddComponent<TimerComponent>();
				Game.Scene.AddComponent<UIComponent>();
				Game.Scene.AddComponent<GamerComponent>();
				Game.Scene.AddComponent<MessageDispatcherComponent>();

				//添加测试组件
				//Game.Scene.AddComponent<OpcodeTestComponent>();
				//Game.Scene.AddComponent<FrameTestComponent>();

				//TestRoom room = ComponentFactory.Create<TestRoom>();
				//room.AddComponent<TimeTestComponent>();
				//room.GetComponent<TimeTestComponent>().Run(Typebehavior.Waiting, 500);
				//room.GetComponent<TimeTestComponent>().Run(Typebehavior.RandTarger);
				Game.EventSystem.Run(UIEventType.LandInitSceneStart);
				//测试发送给服务端一条文本消息
				Session session = Game.Scene.GetComponent<NetOuterComponent>().Create(GlobalConfigComponent.Instance.GlobalProto.Address);
                G2C_TestMessage g2CTestMessage = (G2C_TestMessage) await session.Call(new C2G_TestMessage() { Info = "==>>服务端的朋友,你好!收到请回答" });		
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

		private void Update()
		{
			OneThreadSynchronizationContext.Instance.Update();
			Game.EventSystem.Update();
		}

		private void LateUpdate()
		{
			Game.EventSystem.LateUpdate();
		}

		private void OnApplicationQuit()
		{
			Game.Close();
		}
	}
}