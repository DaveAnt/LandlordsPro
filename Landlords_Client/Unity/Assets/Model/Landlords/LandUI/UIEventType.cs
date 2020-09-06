namespace ETModel
{
    public static partial class LandUIType
    {
        public const string LandEnd = "LandEnd";
        public const string LandRoom = "LandRoom";
        public const string LandLogin = "LandLogin";
        public const string LandLobby = "LandLobby";
        public const string SetUserInfo = "SetUserInfo";
        public const string LandInteraction = "LandInteraction";
    }

    public class UIEventType
    {
        //斗地主EventIdType
        public const string LandInitSetUserInfo = "LandInitSetUserInfo";
        public const string LandSetUserInfoFinish = "LandSetUserInfoFinish";
        public const string LandInitSceneStart = "LandInitSceneStart";
        public const string LandLoginFinish = "LandLoginFinish";
        public const string LandInitLobby = "LandInitLobby";

    }

    [Event(UIEventType.LandInitSceneStart)]
    public class InitSceneStart_CreateLandLogin : AEvent
    {
        public override void Run()
        {
            Game.Scene.GetComponent<UIComponent>().Create(LandUIType.LandLogin);
        }
    }

    [Event(UIEventType.LandInitSetUserInfo)]
    public class LandInit_SetUserInfo : AEvent
    {
        public override void Run()
        {
            Game.Scene.GetComponent<UIComponent>().Create(LandUIType.SetUserInfo);
        }
    }

    [Event(UIEventType.LandSetUserInfoFinish)]
    public class LandFinish_SetUserInfo : AEvent
    {
        public override void Run()
        {
            Game.Scene.GetComponent<UIComponent>().Remove(LandUIType.SetUserInfo);
        }
    }


    [Event(UIEventType.LandLoginFinish)]
    public class LandLoginFinish : AEvent
    {
        public override void Run()
        {
            Game.Scene.GetComponent<UIComponent>().Remove(LandUIType.LandLogin);
        }
    }

    [Event(UIEventType.LandInitLobby)]
    public class LandInitLobby_CreateLandLobby : AEvent
    {
        public override void Run()
        {
            Game.Scene.GetComponent<UIComponent>().Create(LandUIType.LandLobby);
        }
    }
}
