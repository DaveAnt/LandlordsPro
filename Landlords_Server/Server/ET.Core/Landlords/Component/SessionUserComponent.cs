﻿using System.Net;

namespace ETModel
{
    /// <summary>
    /// Gate服务器上 Session关联User对象组件
    /// 用于Session断开时触发下线
    /// </summary>
    public class SessionUserComponent : Component
    {
        //Gate服务器上 Session绑定的User对象
        public User User { get; set; }
    }
}