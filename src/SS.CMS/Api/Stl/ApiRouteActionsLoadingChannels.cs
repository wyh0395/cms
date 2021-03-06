﻿using SS.CMS.Core;

namespace SS.CMS.Api.Stl
{
    public static class ApiRouteActionsLoadingChannels
    {
        public const string Route = "sys/stl/actions/loading_channels";

        public static string GetUrl(string apiUrl)
        {
            apiUrl = PageUtils.Combine(apiUrl, Route);
            return apiUrl;
        }
    }
}