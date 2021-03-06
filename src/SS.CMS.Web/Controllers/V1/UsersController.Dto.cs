﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SS.CMS.Abstractions;

namespace SS.CMS.Web.Controllers.V1
{
    public partial class UsersController
    {
        public class ListRequest
        {
            public int Top { get; set; }
            public int Skip { get; set; }
        }

        public class ListResult
        {
            public int Count { get; set; }
            public List<User> Users { get; set; }
        }

        public class UploadAvatarRequest
        {
            public IFormFile File { get; set; }
        }

        public class LoginRequest
        {
            /// <summary>
            /// 账号
            /// </summary>
            public string Account { get; set; }

            /// <summary>
            /// 密码
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// 下次自动登录
            /// </summary>
            public bool IsAutoLogin { get; set; }
        }

        public class LoginResult
        {
            public User User { get; set; }
            public string AccessToken { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        public class GetLogsRequest
        {
            public int Top { get; set; }
            public int Skip { get; set; }
        }

        public class GetLogsResult
        {
            public int Count { get; set; }
            public List<UserLog> Logs { get; set; }
        }

        public class ResetPasswordRequest
        {
            public string Password { get; set; }
            public string NewPassword { get; set; }
        }
    }
}
