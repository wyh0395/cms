﻿using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using SS.CMS.Abstractions;
using SS.CMS;
using SS.CMS.StlParser.Model;
using SS.CMS.StlParser.Utility;
using SS.CMS.Core;
using SS.CMS.Framework;

namespace SS.CMS.StlParser.StlElement
{
    [StlElement(Title = "显示导航", Description = "通过 stl:navigation 标签在模板中显示链接导航")]
    public class StlNavigation
    {
        private StlNavigation() { }
        public const string ElementName = "stl:navigation";

        [StlAttribute(Title = "类型")]
        private const string Type = nameof(Type);
        
        [StlAttribute(Title = "当无内容时显示的信息")]
        private const string EmptyText = nameof(EmptyText);
        
        [StlAttribute(Title = "导航提示信息")]
        private const string TipText = nameof(TipText);
        
        [StlAttribute(Title = "显示字数")]
        private const string WordNum = nameof(WordNum);
        
        [StlAttribute(Title = "是否开启键盘，↑↓←→键分别为上下左右")]
        private const string IsKeyboard = nameof(IsKeyboard);

        public const string TypePreviousChannel = "PreviousChannel";
        public const string TypeNextChannel = "NextChannel";
        public const string TypePreviousContent = "PreviousContent";
        public const string TypeNextContent = "NextContent";

        public static SortedList<string, string> TypeList => new SortedList<string, string>
        {
            {TypePreviousChannel, "上一栏目链接"},
            {TypeNextChannel, "下一栏目链接"},
            {TypePreviousContent, "上一内容链接"},
            {TypeNextContent, "下一内容链接"}
        };

        public static async Task<object> ParseAsync(PageInfo pageInfo, ContextInfo contextInfo)
        {
            var type = TypeNextContent;
            var emptyText = string.Empty;
            var tipText = string.Empty;
            var wordNum = 0;
            var isKeyboard = false;
            var attributes = new NameValueCollection();

            foreach (var name in contextInfo.Attributes.AllKeys)
            {
                var value = contextInfo.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, Type))
                {
                    type = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, EmptyText))
                {
                    emptyText = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, TipText))
                {
                    tipText = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, WordNum))
                {
                    wordNum = TranslateUtils.ToInt(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, IsKeyboard))
                {
                    isKeyboard = TranslateUtils.ToBool(value);
                }
                else
                {
                    TranslateUtils.AddAttributeIfNotExists(attributes, name, value);
                }
            }

            return await ParseImplAsync(pageInfo, contextInfo, attributes, type, emptyText, tipText, wordNum, isKeyboard);
        }

        private static async Task<string> ParseImplAsync(PageInfo pageInfo, ContextInfo contextInfo, NameValueCollection attributes, string type, string emptyText, string tipText, int wordNum, bool isKeyboard)
        {
            string parsedContent;

            var innerHtml = string.Empty;
            StlParserUtility.GetYesNo(contextInfo.InnerHtml, out var successTemplateString, out var failureTemplateString);

            var contentInfo = await contextInfo.GetContentAsync();

            if (string.IsNullOrEmpty(successTemplateString))
            {
                var nodeInfo = await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId);

                if (type.ToLower().Equals(TypePreviousChannel.ToLower()) || type.ToLower().Equals(TypeNextChannel.ToLower()))
                {
                    var taxis = nodeInfo.Taxis;
                    var isNextChannel = !StringUtils.EqualsIgnoreCase(type, TypePreviousChannel);
                    //var siblingChannelId = DataProvider.ChannelRepository.GetIdByParentIdAndTaxis(node.ParentId, taxis, isNextChannel);
                    var siblingChannelId = await DataProvider.ChannelRepository.GetIdByParentIdAndTaxisAsync(pageInfo.SiteId, nodeInfo.ParentId, taxis, isNextChannel);
                    if (siblingChannelId != 0)
                    {
                        var siblingNodeInfo = await DataProvider.ChannelRepository.GetAsync(siblingChannelId);
                        var url = await PageUtility.GetChannelUrlAsync(pageInfo.Site, siblingNodeInfo, pageInfo.IsLocal);
                        if (url.Equals(PageUtils.UnClickableUrl))
                        {
                            attributes["target"] = string.Empty;
                        }
                        attributes["href"] = url;

                        if (string.IsNullOrEmpty(contextInfo.InnerHtml))
                        {
                            innerHtml = await DataProvider.ChannelRepository.GetChannelNameAsync(pageInfo.SiteId, siblingChannelId);
                            if (wordNum > 0)
                            {
                                innerHtml = WebUtils.MaxLengthText(innerHtml, wordNum);
                            }
                        }
                        else
                        {
                            contextInfo.ChannelId = siblingChannelId;
                            var innerBuilder = new StringBuilder(contextInfo.InnerHtml);
                            await StlParserManager.ParseInnerContentAsync(innerBuilder, pageInfo, contextInfo);
                            innerHtml = innerBuilder.ToString();
                        }
                    }
                }
                else if (type.ToLower().Equals(TypePreviousContent.ToLower()) || type.ToLower().Equals(TypeNextContent.ToLower()))
                {
                    if (contextInfo.ContentId != 0)
                    {
                        var taxis = contentInfo.Taxis;
                        var isNextContent = !StringUtils.EqualsIgnoreCase(type, TypePreviousContent);
                        var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, contextInfo.ChannelId);
                        var siblingContentId = await DataProvider.ContentRepository.GetContentIdAsync(tableName, contextInfo.ChannelId, taxis, isNextContent);
                        if (siblingContentId != 0)
                        {
                            var siblingContentInfo = await DataProvider.ContentRepository.GetAsync(pageInfo.Site, contextInfo.ChannelId, siblingContentId);
                            var url = await PageUtility.GetContentUrlAsync(pageInfo.Site, siblingContentInfo, pageInfo.IsLocal);
                            if (url.Equals(PageUtils.UnClickableUrl))
                            {
                                attributes["target"] = string.Empty;
                            }
                            attributes["href"] = url;

                            if (isKeyboard)
                            {
                                var keyCode = isNextContent ? 39 : 37;
                                var scriptContent = new StringBuilder();
                                await pageInfo.AddPageBodyCodeIfNotExistsAsync(PageInfo.Const.Jquery);
                                scriptContent.Append($@"<script language=""javascript"" type=""text/javascript""> 
      $(document).keydown(function(event){{
        if(event.keyCode=={keyCode}){{location = '{url}';}}
      }});
</script> 
");
                                var nextOrPrevious = isNextContent ? "nextContent" : "previousContent";

                                pageInfo.BodyCodes[nextOrPrevious] = scriptContent.ToString();
                            }

                            if (string.IsNullOrEmpty(contextInfo.InnerHtml))
                            {
                                innerHtml = siblingContentInfo.Title;
                                if (wordNum > 0)
                                {
                                    innerHtml = WebUtils.MaxLengthText(innerHtml, wordNum);
                                }
                            }
                            else
                            {
                                var innerBuilder = new StringBuilder(contextInfo.InnerHtml);
                                contextInfo.ContentId = siblingContentId;
                                await StlParserManager.ParseInnerContentAsync(innerBuilder, pageInfo, contextInfo);
                                innerHtml = innerBuilder.ToString();
                            }
                        }
                    }
                }

                parsedContent = string.IsNullOrEmpty(attributes["href"]) ? emptyText : $@"<a {TranslateUtils.ToAttributesString(attributes)}>{innerHtml}</a>";
            }
            else
            {
                var nodeInfo = await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId);

                var isSuccess = false;
                var theContextInfo = contextInfo.Clone();

                if (type.ToLower().Equals(TypePreviousChannel.ToLower()) || type.ToLower().Equals(TypeNextChannel.ToLower()))
                {
                    var taxis = nodeInfo.Taxis;
                    var isNextChannel = !StringUtils.EqualsIgnoreCase(type, TypePreviousChannel);
                    //var siblingChannelId = DataProvider.ChannelRepository.GetIdByParentIdAndTaxis(node.ParentId, taxis, isNextChannel);
                    var siblingChannelId = await DataProvider.ChannelRepository.GetIdByParentIdAndTaxisAsync(pageInfo.SiteId, nodeInfo.ParentId, taxis, isNextChannel);
                    if (siblingChannelId != 0)
                    {
                        isSuccess = true;
                        theContextInfo.ContextType = ContextType.Channel;
                        theContextInfo.ChannelId = siblingChannelId;
                    }
                }
                else if (type.ToLower().Equals(TypePreviousContent.ToLower()) || type.ToLower().Equals(TypeNextContent.ToLower()))
                {
                    if (contextInfo.ContentId != 0)
                    {
                        var taxis = contentInfo.Taxis;
                        var isNextContent = !StringUtils.EqualsIgnoreCase(type, TypePreviousContent);
                        var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, contextInfo.ChannelId);
                        var siblingContentId = await DataProvider.ContentRepository.GetContentIdAsync(tableName, contextInfo.ChannelId, taxis, isNextContent);
                        if (siblingContentId != 0)
                        {
                            isSuccess = true;
                            theContextInfo.ContextType = ContextType.Content;
                            theContextInfo.ContentId = siblingContentId;
                            theContextInfo.SetContentInfo(null);
                        }
                    }
                }

                parsedContent = isSuccess ? successTemplateString : failureTemplateString;

                if (!string.IsNullOrEmpty(parsedContent))
                {
                    var innerBuilder = new StringBuilder(parsedContent);
                    await StlParserManager.ParseInnerContentAsync(innerBuilder, pageInfo, theContextInfo);

                    parsedContent = innerBuilder.ToString();
                }
            }

            parsedContent = tipText + parsedContent;

            return parsedContent;
        }
    }
}
