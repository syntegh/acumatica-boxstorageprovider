﻿using PX.Common;
using PX.Data;
using System;
using System.Collections;

namespace PX.SM.BoxStorageProvider
{
    public class UserProfile : PXGraph<UserProfile>
    {
        public PXSave<BoxUserTokens> Save;
        public PXSelect<BoxUserTokens, Where<BoxUserTokens.userID, Equal<Current<AccessInfo.userID>>>> User;
        public PXFilter<BoxAuthInfo> AuthInfo;

        public PXAction<BoxUserTokens> login;
        [PXUIField(DisplayName = "Login to Box")]
        [PXButton(ImageKey = "LinkWB")]
        public virtual IEnumerable Login(PXAdapter adapter)
        {
            Actions.PressSave();
            AuthInfo.AskExt();
            return adapter.Get();
        }
        
        public PXAction<BoxUserTokens> completeAuthentication;
        [PXButton(ImageKey = "LinkWB")]
        public virtual IEnumerable CompleteAuthentication(PXAdapter adapter)
        {
            PXLongOperation.StartOperation(this, delegate
            {
                User.Cache.Clear();
                var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
                BoxUserTokens boxUser = PXCache<BoxUserTokens>.CreateCopy(User.Select());
                var userInfo = BoxUtils.GetUserInfo(tokenHandler).Result;
                boxUser.BoxUserID = userInfo.Id;
                boxUser.BoxEmailAddress = userInfo.Login;
                User.Update(boxUser);
                Actions.PressSave();
            });

            return adapter.Get();
        }

        public virtual void BoxUserTokens_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            var user = (BoxUserTokens) e.Row;
            if (string.IsNullOrEmpty(user.BoxUserID))
            {
                user.UserStatus = PXLocalizer.Localize(Messages.NotConfigured);
            }
            else
            {
                if (string.IsNullOrEmpty(user.AccessToken))
                {
                    user.UserStatus = PXLocalizer.Localize(Messages.Expired);
                }
                else
                {
                    user.UserStatus = PXLocalizer.Localize(Messages.Configured);
                }
            }

            Save.SetVisible(false);
        }

        public virtual void BoxUserTokens_RefreshTokenDate_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
        {
            var userToken = e.Row as BoxUserTokens;
            if (userToken != null && userToken.RefreshTokenDate.HasValue)
            {
                e.ReturnValue = PXTimeZoneInfo.ConvertTimeFromUtc(userToken.RefreshTokenDate.Value, PXContext.PXIdentity.TimeZone);
            }
        }

        [Serializable]
        public class BoxAuthInfo : IBqlTable
        {
            public abstract class authTicket : IBqlField { }
            public virtual string AuthTicket { get; set; }
        }
    }
}
