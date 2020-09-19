using System;
using System.Collections.Generic;
using ETModel;
using UnityEngine;

namespace ETModel
{
    [MessageHandler]
    public class Actor_GamersReady_NttHandler : AMHandler<Actor_GamersReady_Landlords>
    {
        protected override async ETTask Run(ETModel.Session session, Actor_GamersReady_Landlords message)
        {
            UI uiRoom = Game.Scene.GetComponent<UIComponent>().Get(LandUIType.LandRoom);
            LandRoomComponent room = uiRoom.GetComponent<LandRoomComponent>();
            foreach (long UserID in message.IsReadyUsers)
            {
                Gamer gamer = room.GetGamer(UserID);
                gamer.GetComponent<LandlordsGamerPanelComponent>().SetReady();
            }
            await ETTask.CompletedTask;
        }
    }
}