﻿using System;

namespace Sitana.Framework.Games.Apis
{
    public abstract class AchievementsHandler
    {
        public event EventHandler<AchievementInfoEventArgs> AchievementInfo;

        public abstract void SendAchievement(String id, Int32 completion);

        public abstract void GetAchievementsList();
    }
}
