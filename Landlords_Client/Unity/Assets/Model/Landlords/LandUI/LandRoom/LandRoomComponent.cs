﻿using System;
using ETModel;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ETModel
{
    [ObjectSystem]
    public class LandRoomComponentAwakeSystem : AwakeSystem<LandRoomComponent>
    {
        public override void Awake(LandRoomComponent self)
        {
            self.Awake();
        }
    }

    /// <summary>
    /// 大厅界面组件
    /// </summary>
    public class LandRoomComponent : Component
    {
        public readonly Dictionary<long, int> seats = new Dictionary<long, int>();
        public bool Matching { get; set; }
        public readonly Gamer[] gamers = new Gamer[3];
        public static Gamer LocalGamer { get; private set; }

        public Text prompt;
        public Text multiples;
        public readonly GameObject[] GamersPanel = new GameObject[3];
        public LandInteractionComponent interaction;
        public LandInteractionComponent Interaction
        {
            get
            {
                if (interaction == null)
                {
                    UI uiRoom = this.GetParent<UI>();
                    UI uiInteraction = LandInteractionFactory.Create(LandUIType.LandInteraction, uiRoom);
                    interaction = uiInteraction.GetComponent<LandInteractionComponent>();
                }
                return interaction;
            }
        }

        public void Awake()
        {
            ReferenceCollector rc = this.GetParent<UI>().GameObject.GetComponent<ReferenceCollector>();

            GameObject quitButton = rc.Get<GameObject>("Quit");
            GameObject readyButton = rc.Get<GameObject>("Ready");
            prompt = rc.Get<GameObject>("MatchPrompt").GetComponent<Text>();
            multiples = rc.Get<GameObject>("Multiples").GetComponent<Text>();
            GameObject gamersPanel = rc.Get<GameObject>("Gamers");
            this.GamersPanel[0] = gamersPanel.Get<GameObject>("Left");
            this.GamersPanel[1] = gamersPanel.Get<GameObject>("Local");
            this.GamersPanel[2] = gamersPanel.Get<GameObject>("Right");

            readyButton.SetActive(false); //默认隐藏
            Matching = true; //进入房间后取消匹配状态

            //绑定事件
            quitButton.GetComponent<Button>().onClick.Add(OnQuit);
            readyButton.GetComponent<Button>().onClick.Add(OnReady);

            //添加本地玩家
            Gamer gamer = ETModel.ComponentFactory.Create<Gamer, long>(GamerComponent.Instance.MyUser.UserID);
            AddGamer(gamer, 1);
            LocalGamer = gamer;
        }

        public void AddGamer(Gamer gamer, int index)
        {
            seats.Add(gamer.UserID, index);
            gamers[index] = gamer;
            gamer.AddComponent<LandlordsGamerPanelComponent>().SetPanel(GamersPanel[index]);
            prompt.text = $"一位玩家进入房间，房间人数{seats.Count}";
        }

        public void RemoveGamer(long id)
        {
            int seatIndex = GetGamerSeat(id);
            if (seatIndex >= 0)
            {
                Gamer gamer = gamers[seatIndex];
                gamers[seatIndex] = null;
                seats.Remove(id);
                gamer.Dispose();
                prompt.text = $"一位玩家离开房间，房间人数{seats.Count}";
            }
        }

        public Gamer GetGamer(long id)
        {
            int seatIndex = GetGamerSeat(id);
            if (seatIndex >= 0)
            {
                return gamers[seatIndex];
            }

            return null;
        }

        public void SetMultiples(int multiples)
        {
            this.multiples.gameObject.SetActive(true);
            this.multiples.text = multiples.ToString();
        }

        public void ResetMultiples()
        {
            this.multiples.gameObject.SetActive(false);
            this.multiples.text = "1";
        }

        public int GetGamerSeat(long id)
        {
            int seatIndex;
            if (seats.TryGetValue(id, out seatIndex))
            {
                return seatIndex;
            }
            return -1;
        }

        public void OnQuit()
        {
            //发送退出房间消息
            SessionComponent.Instance.Session.Send(new C2G_ReturnLobby_Ntt());

            //切换到大厅界面
            Game.Scene.GetComponent<UIComponent>().Create(LandUIType.LandLobby);
            Game.Scene.GetComponent<UIComponent>().Remove(LandUIType.LandRoom);
        }

        private void OnReady()
        {
            //发送准备
            Debug.Log("Actor_GamerReady_Landlords");
            SessionComponent.Instance.Session.Send(new Actor_GamerReady_Landlords());
        }

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            base.Dispose();

            this.Matching = false;
            LocalGamer = null;
            this.seats.Clear();

            for (int i = 0; i < this.gamers.Length; i++)
            {
                if (gamers[i] != null)
                {
                    gamers[i].Dispose();
                    gamers[i] = null;
                }
            }
        }
    }
}