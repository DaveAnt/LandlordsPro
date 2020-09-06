using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETModel
{
    [ObjectSystem]
    public class FrameTestComponentUpdateSystem : UpdateSystem<FrameTestComponent>
    {
        public override void Update(FrameTestComponent self)
        {
            self.Update();
        }
    }
    public class FrameTestComponent:Component
    {
        public int count = 1;
        public int waitTime = 1000;
        public bool interval = false;

        public void Update()
        {
            if (interval)
                return;
            this.UpdateAsync().Coroutine();
        }

        public async ETVoid UpdateAsync()
        {
            Log.Info($"===>frame{count}");
            interval = true;
            count++;
            TimerComponent timerComponent = Game.Scene.GetComponent<TimerComponent>();
            await timerComponent.WaitAsync(waitTime);
            interval = false;
        }
    }
}
