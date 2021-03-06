﻿using System.Collections.Generic;
using System.Text;
using SS.CMS.Abstractions;
using SS.CMS.StlParser.Model;
using SS.CMS.StlParser.Parsers;
using SS.CMS.StlParser.StlEntity;
using SS.CMS.StlParser.Utility;
using System.Threading.Tasks;
using SS.CMS.Api.Stl;
using SS.CMS.Framework;

namespace SS.CMS.StlParser.StlElement
{
    [StlElement(Title = "动态显示", Description = "通过 stl:dynamic 标签在模板中实现动态显示功能")]
    public class StlDynamic
    {
        private StlDynamic() { }
        public const string ElementName = "stl:dynamic";

        [StlAttribute(Title = "所处上下文")]
        private const string Context = nameof(Context);

        [StlAttribute(Title = "显示模式")]
        private const string Inline = nameof(Inline);

        [StlAttribute(Title = "动态请求发送前执行的JS代码")]
        private const string OnBeforeSend = nameof(OnBeforeSend);

        [StlAttribute(Title = "动态请求成功后执行的JS代码")]
        private const string OnSuccess = nameof(OnSuccess);

        [StlAttribute(Title = "动态请求结束后执行的JS代码")]
        private const string OnComplete = nameof(OnComplete);

        [StlAttribute(Title = "动态请求失败后执行的JS代码")]
        private const string OnError = nameof(OnError);

        internal static async Task<object> ParseAsync(PageInfo pageInfo, ContextInfo contextInfo)
        {
            // 如果是实体标签则返回空
            if (contextInfo.IsStlEntity)
            {
                return string.Empty;
            }

            var inline = false;
            var onBeforeSend = string.Empty;
            var onSuccess = string.Empty;
            var onComplete = string.Empty;
            var onError = string.Empty;

            foreach (var name in contextInfo.Attributes.AllKeys)
            {
                var value = contextInfo.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, Context))
                {
                    contextInfo.ContextType = TranslateUtils.ToEnum(value, ContextType.Undefined);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Inline))
                {
                    inline = TranslateUtils.ToBool(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, OnBeforeSend))
                {
                    onBeforeSend = await StlEntityParser.ReplaceStlEntitiesForAttributeValueAsync(value, pageInfo, contextInfo);
                }
                else if (StringUtils.EqualsIgnoreCase(name, OnSuccess))
                {
                    onSuccess = await StlEntityParser.ReplaceStlEntitiesForAttributeValueAsync(value, pageInfo, contextInfo);
                }
                else if (StringUtils.EqualsIgnoreCase(name, OnComplete))
                {
                    onComplete = await StlEntityParser.ReplaceStlEntitiesForAttributeValueAsync(value, pageInfo, contextInfo);
                }
                else if (StringUtils.EqualsIgnoreCase(name, OnError))
                {
                    onError = await StlEntityParser.ReplaceStlEntitiesForAttributeValueAsync(value, pageInfo, contextInfo);
                }
            }

            StlParserUtility.GetLoading(contextInfo.InnerHtml, out var loading, out var template);
            if (!string.IsNullOrEmpty(loading))
            {
                var innerBuilder = new StringBuilder(loading);
                await StlParserManager.ParseInnerContentAsync(innerBuilder, pageInfo, contextInfo);
                loading = innerBuilder.ToString();
            }

            return await ParseImplAsync(pageInfo, contextInfo, loading, template, inline, onBeforeSend, onSuccess, onComplete, onError);
        }

        private static async Task<string> ParseImplAsync(PageInfo pageInfo, ContextInfo contextInfo, string loading, string template, bool inline, string onBeforeSend, string onSuccess, string onComplete, string onError)
        {
            await pageInfo.AddPageBodyCodeIfNotExistsAsync(PageInfo.Const.StlClient);

            //运行解析以便为页面生成所需JS引用
            if (!string.IsNullOrEmpty(template))
            {
                await StlParserManager.ParseInnerContentAsync(new StringBuilder(template), pageInfo, contextInfo);
            }

            var dynamicInfo = new DynamicInfo
            {
                ElementName = ElementName,
                SiteId = pageInfo.SiteId,
                ChannelId = contextInfo.ChannelId,
                ContentId = contextInfo.ContentId,
                TemplateId = pageInfo.Template.Id,
                AjaxDivId = StlParserUtility.GetAjaxDivId(pageInfo.UniqueId),
                LoadingTemplate = loading,
                SuccessTemplate = template,
                OnBeforeSend = onBeforeSend,
                OnSuccess = onSuccess,
                OnComplete = onComplete,
                OnError = onError
            };

            return dynamicInfo.GetScript(ApiRouteActionsDynamic.GetUrl(pageInfo.ApiUrl), inline);
        }

        internal static async Task<string> ParseDynamicElementAsync(string stlElement, PageInfo pageInfo, ContextInfo contextInfo)
        {
            stlElement = StringUtils.ReplaceIgnoreCase(stlElement, "isdynamic=\"true\"", string.Empty);
            return await ParseImplAsync(pageInfo, contextInfo, string.Empty, stlElement, true, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public static async Task<string> ParseDynamicContentAsync(DynamicInfo dynamicInfo, string template)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            var templateInfo = await DataProvider.TemplateRepository.GetAsync(dynamicInfo.TemplateId);
            var siteInfo = await DataProvider.SiteRepository.GetAsync(dynamicInfo.SiteId);
            var pageInfo = await PageInfo.GetPageInfoAsync(dynamicInfo.ChannelId, dynamicInfo.ContentId, siteInfo,
                templateInfo, new Dictionary<string, object>());

            pageInfo.UniqueId = 1000;
            pageInfo.User = dynamicInfo.User;

            var contextInfo = new ContextInfo(pageInfo);

            var templateContent = StlRequestEntities.ParseRequestEntities(dynamicInfo.QueryString, template);
            var contentBuilder = new StringBuilder(templateContent);
            var stlElementList = StlParserUtility.GetStlElementList(contentBuilder.ToString());

            var pageIndex = dynamicInfo.Page - 1;

            //如果标签中存在<stl:pageContents>
            if (StlParserUtility.IsStlElementExists(StlPageContents.ElementName, stlElementList))
            {
                var stlElement = StlParserUtility.GetStlElement(StlPageContents.ElementName, stlElementList);
                var stlPageContentsElement = stlElement;
                var stlPageContentsElementReplaceString = stlElement;

                var pageContentsElementParser = await StlPageContents.GetAsync(stlPageContentsElement, pageInfo, contextInfo);
                var (pageCount, totalNum) = pageContentsElementParser.GetPageCount();

                for (var currentPageIndex = 0; currentPageIndex < pageCount; currentPageIndex++)
                {
                    if (currentPageIndex == pageIndex)
                    {
                        var pageHtml = await pageContentsElementParser.ParseAsync(totalNum, currentPageIndex, pageCount, false);
                        contentBuilder.Replace(stlPageContentsElementReplaceString, pageHtml);

                        await StlParserManager.ReplacePageElementsInDynamicPageAsync(contentBuilder, pageInfo, stlElementList, currentPageIndex, pageCount, totalNum, false, dynamicInfo.AjaxDivId);

                        break;
                    }
                }
            }
            //如果标签中存在<stl:pageChannels>
            else if (StlParserUtility.IsStlElementExists(StlPageChannels.ElementName, stlElementList))
            {
                var stlElement = StlParserUtility.GetStlElement(StlPageChannels.ElementName, stlElementList);
                var stlPageChannelsElement = stlElement;
                var stlPageChannelsElementReplaceString = stlElement;

                var pageChannelsElementParser = await StlPageChannels.GetAsync(stlPageChannelsElement, pageInfo, contextInfo);
                var pageCount = pageChannelsElementParser.GetPageCount(out var totalNum);

                for (var currentPageIndex = 0; currentPageIndex < pageCount; currentPageIndex++)
                {
                    if (currentPageIndex != pageIndex) continue;

                    var pageHtml = await pageChannelsElementParser.ParseAsync(currentPageIndex, pageCount);
                    contentBuilder.Replace(stlPageChannelsElementReplaceString, pageHtml);

                    await StlParserManager.ReplacePageElementsInDynamicPageAsync(contentBuilder, pageInfo, stlElementList, currentPageIndex, pageCount, totalNum, false, dynamicInfo.AjaxDivId);

                    break;
                }
            }
            //如果标签中存在<stl:pageSqlContents>
            else if (StlParserUtility.IsStlElementExists(StlPageSqlContents.ElementName, stlElementList))
            {
                var stlElement = StlParserUtility.GetStlElement(StlPageSqlContents.ElementName, stlElementList);
                var stlPageSqlContentsElement = stlElement;
                var stlPageSqlContentsElementReplaceString = stlElement;

                var pageSqlContentsElementParser = await StlPageSqlContents.GetAsync(stlPageSqlContentsElement, pageInfo, contextInfo);
                var pageCount = pageSqlContentsElementParser.GetPageCount(out var totalNum);

                for (var currentPageIndex = 0; currentPageIndex < pageCount; currentPageIndex++)
                {
                    if (currentPageIndex != pageIndex) continue;

                    var pageHtml = await pageSqlContentsElementParser.ParseAsync(totalNum, currentPageIndex, pageCount, false);
                    contentBuilder.Replace(stlPageSqlContentsElementReplaceString, pageHtml);

                    await StlParserManager.ReplacePageElementsInDynamicPageAsync(contentBuilder, pageInfo, stlElementList, currentPageIndex, pageCount, totalNum, false, dynamicInfo.AjaxDivId);

                    break;
                }
            }

            else if (StlParserUtility.IsStlElementExists(StlPageItems.ElementName, stlElementList))
            {
                var pageCount = TranslateUtils.ToInt(dynamicInfo.QueryString["pageCount"]);
                var totalNum = TranslateUtils.ToInt(dynamicInfo.QueryString["totalNum"]);
                var pageContentsAjaxDivId = dynamicInfo.QueryString["pageContentsAjaxDivId"];

                for (var currentPageIndex = 0; currentPageIndex < pageCount; currentPageIndex++)
                {
                    if (currentPageIndex != pageIndex) continue;

                    await StlParserManager.ReplacePageElementsInDynamicPageAsync(contentBuilder, pageInfo, stlElementList, currentPageIndex, pageCount, totalNum, false, pageContentsAjaxDivId);

                    break;
                }
            }

            await StlParserManager.ParseInnerContentAsync(contentBuilder, pageInfo, contextInfo);

            //var parsedContent = StlParserUtility.GetBackHtml(contentBuilder.ToString(), pageInfo);
            //return pageInfo.HeadCodesHtml + pageInfo.BodyCodesHtml + parsedContent + pageInfo.FootCodesHtml;
            return contentBuilder.ToString();
        }
    }
}
