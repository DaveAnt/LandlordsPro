﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETModel
{
    [TimeBehavior(Typebehavior.RandTarger)]
    public class RandTargetTimeBehavior : ITimeBehavior
    {
        public TestRoom room;
        public Random rd = new Random();
        public int count = 0;
        public int maxCount;
        public void Behavior(Entity parent, long time)
        {
            room = parent as TestRoom;
            StartGame();
        }

        private void StartGame()
        {
            room.gamers.Add(1, "gamer01");
            room.gamers.Add(2, "gamer02");
            room.gamers.Add(3, "gamer03");
            room.gamers.Add(4, "gamer04");
            room.gamers.Add(5, "gamer05");

            maxCount = rd.Next(2, 4);//随机点名
            Log.Info($"将要点名字{maxCount}次");
            RandTimeAndTarget().Coroutine();
        }

        private async ETVoid RandTimeAndTarget()
        {
            int num = rd.Next(1, 5);
            string target = room.gamers[num];
            int randtime = rd.Next(3, 12);

            TimerComponent timer = Game.Scene.GetComponent<TimerComponent>();
            room.randCts = new System.Threading.CancellationTokenSource();
            await timer.WaitAsync((randtime + 1) * 1000, room.randCts.Token);
            Log.Info($"{room.GetType().ToString()}-执行间隔{randtime}秒点名{target}");
            room.randCts.Dispose();
            room.randCts = null;

            count++;
            if (count < maxCount)
                RandTimeAndTarget().Coroutine();
        }
    }
}
