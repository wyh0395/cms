﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SS.CMS.Abstractions;
using SS.CMS;
using SS.CMS.StlParser.Model;
using SS.CMS.Core;

namespace SS.CMS.StlParser.StlElement
{
    [StlElement(Title = "播放视频", Description = "通过 stl:video 标签在模板中显示视频播放器")]
    public class StlVideo
	{
        private StlVideo() { }
		public const string ElementName = "stl:video";

        [StlAttribute(Title = "指定视频的字段")]
        private const string Type = nameof(Type);
         
		[StlAttribute(Title = "视频地址")]
        private const string PlayUrl = nameof(PlayUrl);
         
        [StlAttribute(Title = "图片地址")]
        private const string ImageUrl = nameof(ImageUrl);
         
        [StlAttribute(Title = "宽度")]
        private const string Width = nameof(Width);
         
        [StlAttribute(Title = "高度")]
        private const string Height = nameof(Height);
         
        [StlAttribute(Title = "是否自动播放")]
        private const string IsAutoPlay = nameof(IsAutoPlay);
         
        [StlAttribute(Title = "是否显示播放控件")]
        private const string IsControls = nameof(IsControls);
         
        [StlAttribute(Title = "是否循环播放")]
        private const string IsLoop = nameof(IsLoop);

        public static async Task<object> ParseAsync(PageInfo pageInfo, ContextInfo contextInfo)
		{
            var type = ContentAttribute.VideoUrl;
            var playUrl = string.Empty;
            var imageUrl = string.Empty;
            var width = string.Empty;
            var height = string.Empty;
            var isAutoPlay = true;
            var isControls = true;
            var isLoop = false;

            foreach (var name in contextInfo.Attributes.AllKeys)
            {
                var value = contextInfo.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, Type))
                {
                    type = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, PlayUrl))
                {
                    playUrl = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, ImageUrl))
                {
                    imageUrl = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, Width))
                {
                    width = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, Height))
                {
                    height = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, IsAutoPlay))
                {
                    isAutoPlay = TranslateUtils.ToBool(value, true);
                }
                else if (StringUtils.EqualsIgnoreCase(name, IsControls))
                {
                    isControls = TranslateUtils.ToBool(value, true);
                }
                else if (StringUtils.EqualsIgnoreCase(name, IsLoop))
                {
                    isLoop = TranslateUtils.ToBool(value, false);
                }
            }

            return await ParseImplAsync(pageInfo, contextInfo, type, playUrl, imageUrl, width, height, isAutoPlay, isControls, isLoop);
		}

        private static async Task<string> ParseImplAsync(PageInfo pageInfo, ContextInfo contextInfo, string type, string playUrl, string imageUrl, string width, string height, bool isAutoPlay, bool isControls, bool isLoop)
        {
            var videoUrl = string.Empty;
            if (!string.IsNullOrEmpty(playUrl))
            {
                videoUrl = playUrl;
            }
            else
            {
                var contentId = contextInfo.ContentId;
                if (contextInfo.ContextType == ContextType.Content)
                {
                    if (contentId != 0)//获取内容视频
                    {
                        var contentInfo = await contextInfo.GetContentAsync();
                        if (contentInfo != null)
                        {
                            videoUrl = contentInfo.Get<string>(type);
                            if (string.IsNullOrEmpty(videoUrl))
                            {
                                videoUrl = contentInfo.Get<string>(ContentAttribute.VideoUrl);
                            }
                        }
                    }
                }
                else if (contextInfo.ContextType == ContextType.Each)
                {
                    videoUrl = contextInfo.ItemContainer.EachItem.Value as string;
                }
            }

            if (string.IsNullOrEmpty(videoUrl)) return string.Empty;

            videoUrl = await PageUtility.ParseNavigationUrlAsync(pageInfo.Site, videoUrl, pageInfo.IsLocal);
            imageUrl = await PageUtility.ParseNavigationUrlAsync(pageInfo.Site, imageUrl, pageInfo.IsLocal);

            await pageInfo.AddPageBodyCodeIfNotExistsAsync(PageInfo.Const.JsAcVideoJs);

            var dict = new Dictionary<string, string>
            {
                {"class", "video-js vjs-default-skin"},
                {"src", videoUrl}
            };
            if (isAutoPlay)
            {
                dict.Add("autoplay", null);
            }
            if (isControls)
            {
                dict.Add("controls", null);
            }
            if (isLoop)
            {
                dict.Add("loop", null);
            }
            if (!string.IsNullOrEmpty(imageUrl))
            {
                dict.Add("poster", imageUrl);
            }
            if (!string.IsNullOrEmpty(width))
            {
                dict.Add("width", width);
            }
            dict.Add("height", string.IsNullOrEmpty(height) ? "280" : height);

            return $@"<video {TranslateUtils.ToAttributesString(dict)}></video>";
        }
	}
}
