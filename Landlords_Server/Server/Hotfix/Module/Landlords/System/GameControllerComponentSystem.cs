﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ETModel;

namespace ETHotfix
{
    [ObjectSystem]
    public class GameControllerComponentAwakeSystem : AwakeSystem<GameControllerComponent, RoomConfig>
    {
        public override void Awake(GameControllerComponent self, RoomConfig config)
        {
            self.Awake(config);
        }
    }

    public static class GameControllerComponentSystem
    {
        public static void Awake(this GameControllerComponent self, RoomConfig config)
        {
            self.Config = config;
            self.BasePointPerMatch = config.BasePointPerMatch;
            self.Multiples = config.Multiples;
            self.MinThreshold = config.MinThreshold;
        }

        /// <summary>
        /// 准备开始游戏
        /// </summary>
        public static void StartGame(this GameControllerComponent self)
        {
            Room room = self.GetParent<Room>();
            Gamer[] gamers = room.gamers;

            //初始玩家开始状态
            foreach (var _gamer in gamers)
            {
                if (_gamer.GetComponent<HandCardsComponent>() == null)
                {
                    _gamer.AddComponent<HandCardsComponent>();
                }
            }

            GameControllerComponent gameController = room.GetComponent<GameControllerComponent>();
            //洗牌发牌
            gameController.DealCards();

            List<GamerCardNum> gamersCardNum = new List<GamerCardNum>();
            Array.ForEach(gamers, (g) =>
            {
                HandCardsComponent handCards = g.GetComponent<HandCardsComponent>();
                //重置玩家身份
                handCards.AccessIdentity = Identity.None;
                //重置玩家手牌数
                gamersCardNum.Add(new GamerCardNum()
                {
                    UserID = g.UserID,
                    Num = g.GetComponent<HandCardsComponent>().GetAll().Length
                });
            });

            //发送玩家手牌和玩家手牌数
            foreach (var _gamer in gamers)
            {
                ActorMessageSenderComponent actorProxyComponent = Game.Scene.GetComponent<ActorMessageSenderComponent>();
                ActorMessageSender actorProxy = actorProxyComponent.Get(_gamer.CActorID);

                actorProxy.Send(new Actor_GameStartHandCards_Ntt()
                {
                    HandCards = To.RepeatedField(_gamer.GetComponent<HandCardsComponent>().GetAll()),
                    GamersCardNum = To.RepeatedField(gamersCardNum)
                });
            }

            //随机先手玩家
            gameController.RandomFirstAuthority();

            Log.Info($"房间{room.Id}开始游戏");
        }

        /// <summary>
        /// 轮流发牌
        /// </summary>
        public static void DealCards(this GameControllerComponent self)
        {
            Room room = self.GetParent<Room>();

            //牌库洗牌
            room.GetComponent<DeckComponent>().Shuffle();

            //玩家轮流发牌
            Gamer[] gamers = room.gamers;
            int index = 0;
            for (int i = 0; i < 51; i++)
            {
                if (index == 3)
                {
                    index = 0;
                }
                self.DealTo(gamers[index].UserID);
                index++;
            }

            //发地主牌
            for (int i = 0; i < 3; i++)
            {
                //出牌缓存一开始是空的可借用一下来缓存本局所发的地主牌
                self.DealTo(room.Id);
            }
            self.Multiples = self.Config.Multiples;
        }

        /// <summary>
        /// 发牌
        /// </summary>
        public static void DealTo(this GameControllerComponent self, long id)
        {
            Room room = self.GetParent<Room>();
            Card card = room.GetComponent<DeckComponent>().Deal();

            //@@@ 替换了上节暂用存地主牌方法
            //如果id为roomid,说明是发地主牌,不是给玩家id发牌
            if (id == room.Id)
            {
                DeskCardsCacheComponent deskCardsCache = room.GetComponent<DeskCardsCacheComponent>();
                deskCardsCache.AddCard(card);
                deskCardsCache.LordCards.Add(card);
            }
            else
            {
                foreach (var gamer in room.gamers)
                {
                    if (id == gamer.UserID)
                    {
                        gamer.GetComponent<HandCardsComponent>().AddCard(card);
                        break;
                    }
                }
            }

        }

        /// <summary>
        /// 给牌桌发地主牌
        /// </summary>
        public static void CardsOnTable(this GameControllerComponent self, long id)
        {
            Room room = self.GetParent<Room>();
            DeskCardsCacheComponent deskCardsCache = room.GetComponent<DeskCardsCacheComponent>();
            OrderControllerComponent orderController = room.GetComponent<OrderControllerComponent>();
            HandCardsComponent handCards = room.GetGamerFromUserID(id).GetComponent<HandCardsComponent>();

            orderController.Start(id);

            for (int i = 0; i < 3; i++)
            {
                //@@@ 替换了上节暂用发地主牌方法，出牌缓存中移除了地主牌缓存
                Card card = deskCardsCache.Deal();
                //将地主牌添加到地主玩家的手牌
                handCards.AddCard(card);
            }

            //更新玩家身份
            foreach (var gamer in room.gamers)
            {
                Identity gamerIdentity = gamer.UserID == id ? Identity.Landlord : Identity.Farmer;
                self.UpdateInIdentity(gamer, gamerIdentity);
            }

            //广播地主消息
            room.Broadcast(new Actor_SetLandlord_Ntt() { UserID = id, LordCards = To.RepeatedField(deskCardsCache.LordCards) });

            //广播地主先手出牌消息(先手显示出牌交互操作界面)
            room.Broadcast(new Actor_AuthorityPlayCard_Ntt() { UserID = id, IsFirst = true });
        }

        /// <summary>
        /// 更新身份
        /// </summary>
        public static void UpdateInIdentity(this GameControllerComponent self, Gamer gamer, Identity identity)
        {
            gamer.GetComponent<HandCardsComponent>().AccessIdentity = identity;
        }

        /// <summary>
        /// 场上的所有牌回到牌库中
        /// </summary>
        public static void BackToDeck(this GameControllerComponent self)
        {
            Room room = self.GetParent<Room>();
            DeckComponent deckComponent = room.GetComponent<DeckComponent>();

            //回收玩家手牌
            foreach (var gamer in room.gamers)
            {
                HandCardsComponent handCards = gamer.GetComponent<HandCardsComponent>();
                while (handCards.CardsCount > 0)
                {
                    Card card = handCards.library[handCards.CardsCount - 1];
                    handCards.PopCard(card);
                    deckComponent.AddCard(card);
                }
            }
        }

        /// <summary>
        /// 随机先手玩家
        /// </summary>
        public static void RandomFirstAuthority(this GameControllerComponent self)
        {
            Room room = self.GetParent<Room>();
            OrderControllerComponent orderController = room.GetComponent<OrderControllerComponent>();
            Gamer[] gamers = room.gamers;

            int index = RandomHelper.RandomNumber(0, gamers.Length);
            long firstAuthority = gamers[index].UserID;
            orderController.Init(firstAuthority);

            //广播先手抢地主玩家
            room.Broadcast(new Actor_AuthorityGrabLandlord_Ntt() { UserID = firstAuthority });
        }

        /// <summary>
        /// 判断出牌后游戏继续or结束
        /// </summary>
        public static void Continue(this GameControllerComponent self, Gamer lastGamer)
        {
            Room room = self.GetParent<Room>();
            OrderControllerComponent orderController = room.GetComponent<OrderControllerComponent>();

            //测试游戏结束
            //lastGamer.GetComponent<HandCardsComponent>().library.Clear();
            //是否结束,当前出牌者手牌数为0时游戏结束
            bool isEnd = lastGamer.GetComponent<HandCardsComponent>().CardsCount == 0;
            if (isEnd)
            {
                //当前最大出牌者为赢家
                Identity winnerIdentity = room.GetGamerFromUserID(orderController.Biggest).GetComponent<HandCardsComponent>().AccessIdentity;
                List<GamerScore> gamersScore = new List<GamerScore>();

                //游戏结束所有玩家摊牌
                foreach (var gamer in room.gamers)
                {
                    //计算玩家积分
                    gamersScore.Add(new GamerScore()
                    {
                        UserID = gamer.UserID,
                        Score = self.GetGamerScore(gamer, winnerIdentity)
                    });

                    if (gamer.UserID != lastGamer.UserID)
                    {
                        //剩余玩家摊牌
                        Card[] _gamerCards = gamer.GetComponent<HandCardsComponent>().GetAll();
                        room.Broadcast(new Actor_GamerPlayCard_Ntt() { UserID = gamer.UserID, Cards = To.RepeatedField(_gamerCards) });
                    }
                }

                self.GameOver(gamersScore, winnerIdentity);
            }
            else
            {
                //轮到下位玩家出牌
                orderController.Biggest = lastGamer.UserID;
                orderController.Turn();
                room.Broadcast(new Actor_AuthorityPlayCard_Ntt() { UserID = orderController.CurrentAuthority, IsFirst = false });
            }
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        /// <param name="self"></param>
        public static async void GameOver(this GameControllerComponent self, List<GamerScore> gamersScore, Identity winnerIdentity)
        {
            Room room = self.GetParent<Room>();
            Gamer[] gamers = room.gamers;

            //清理所有卡牌
            self.BackToDeck();
            room.GetComponent<DeskCardsCacheComponent>().Clear();

            Dictionary<long, long> gamersMoney = new Dictionary<long, long>();
            foreach (GamerScore gamerScore in gamersScore)
            {
                //结算玩家余额
                Gamer gamer = room.GetGamerFromUserID(gamerScore.UserID);
                long gamerMoney = await self.StatisticalIntegral(gamer, gamerScore.Score);
                gamersMoney[gamer.UserID] = gamerMoney;
            }

            //广播游戏结束消息
            room.Broadcast(new Actor_Gameover_Ntt()
            {
                Winner = (byte)winnerIdentity,
                BasePointPerMatch = self.BasePointPerMatch,
                Multiples = self.Multiples,
                GamersScore = To.RepeatedField(gamersScore)
            });

            //清理房间玩家
            LandMatchComponent Match = Game.Scene.GetComponent<LandMatchComponent>();
            foreach (var _gamer in gamers)
            {
                //踢出离线玩家
                if (_gamer.isOffline)
                {
                    Match.Playing.Remove(_gamer.UserID);
                    _gamer.Dispose();
                }
                //踢出余额不足玩家
                else if (gamersMoney[_gamer.UserID] < self.MinThreshold)
                {
                    //...
                }
                else
                {
                    //修改玩家匹配状态
                    Match.Playing.Remove(_gamer.UserID);
                    Match.Waiting.Add(_gamer.UserID, room);
                }
            }

            GameoverRoomWaiting(room).Coroutine();
        }

        /// <summary>
        /// 游戏结束房间等待
        /// </summary>
        public static async ETVoid GameoverRoomWaiting(Room room)
        {
            //只处理两种情况:1 有玩家选择继续；2 等待两分钟,无玩家继续

            LandMatchComponent Match = Game.Scene.GetComponent<LandMatchComponent>();

            //更改房间状态为空闲房间   
            Match.GamingLandlordsRooms.Remove(room.Id);
            Match.FreeLandlordsRooms.Add(room.Id, room);

            //有玩家继续，通过room的cts取消清房waiting,保留房间
            //玩家继续的handler中调用room.CancellationTokenSource?.Cancel();
            //等待2分钟，无玩家继续，清除玩家与房间
            TimerComponent timer = Game.Scene.GetComponent<TimerComponent>();
            room.CancellationTokenSource = new CancellationTokenSource();
            await timer.WaitAsync(120000, room.CancellationTokenSource.Token);

            //广播消息给房间内玩家客户端通知房间清除返回大厅界面
            //...

            room.CancellationTokenSource.Dispose();
            room.CancellationTokenSource = null;
            room.ClearRoom();
        }

        /// <summary>
        /// 计算玩家积分
        /// </summary>
        public static long GetGamerScore(this GameControllerComponent self, Gamer gamer, Identity winnerIdentity)
        {
            HandCardsComponent handCards = gamer.GetComponent<HandCardsComponent>();

            //积分计算公式：全场底分 * 全场倍率 * 身份倍率
            long integration = self.BasePointPerMatch * self.Multiples * (int)handCards.AccessIdentity;
            //当玩家不是胜者，结算积分为负
            if (handCards.AccessIdentity != winnerIdentity)
                integration = -integration;
            return integration;
        }

        /// <summary>
        /// 结算用户余额保存
        /// </summary>
        public static async Task<long> StatisticalIntegral(this GameControllerComponent self, Gamer gamer, long sorce)
        {
            DBProxyComponent dbProxy = Game.Scene.GetComponent<DBProxyComponent>();

            //结算用户余额
            UserInfo userInfo = await dbProxy.Query<UserInfo>(gamer.UserID);
            userInfo.Money = userInfo.Money + sorce < 0 ? 0 : userInfo.Money + sorce;

            //更新用户信息
            await dbProxy.Save(userInfo);

            return userInfo.Money;
        }

    }
}