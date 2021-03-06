﻿using System;
using System.Collections.Generic;
using System.Text;
using SS.CMS.Abstractions;
using SS.CMS.StlParser.Model;
using SS.CMS.StlParser.Parsers;
using SS.CMS.StlParser.Utility;
using System.Threading.Tasks;
using Datory;
using Datory.Utils;
using SS.CMS;
using SS.CMS.Api.Stl;
using SS.CMS.Core;
using SS.CMS.Framework;

namespace SS.CMS.StlParser.StlElement
{
    [StlElement(Title = "条件判断", Description = "通过 stl:if 标签在模板中根据条件判断显示内容")]
    public class StlIf
    {
        private StlIf() { }
        public const string ElementName = "stl:if";

        [StlAttribute(Title = "测试类型")]
        private const string Type = nameof(Type);

        [StlAttribute(Title = "测试操作")]
        private const string Op = nameof(Op);

        [StlAttribute(Title = "测试值")]
        private const string Value = nameof(Value);

        [StlAttribute(Title = "所处上下文")]
        private const string Context = nameof(Context);

        [StlAttribute(Title = "动态请求发送前执行的JS代码")]
        private const string OnBeforeSend = nameof(OnBeforeSend);

        [StlAttribute(Title = "动态请求成功后执行的JS代码")]
        private const string OnSuccess = nameof(OnSuccess);

        [StlAttribute(Title = "动态请求结束后执行的JS代码")]
        private const string OnComplete = nameof(OnComplete);

        [StlAttribute(Title = "动态请求失败后执行的JS代码")]
        private const string OnError = nameof(OnError);

        public const string TypeIsUserLoggin = "IsUserLoggin";                                      //用户是否已登录
        public const string TypeIsAdministratorLoggin = "IsAdministratorLoggin";                    //管理员是否已登录
        public const string TypeIsUserOrAdministratorLoggin = "IsUserOrAdministratorLoggin";        //用户或管理员是否已登录
        private const string TypeChannelName = "ChannelName";			                            //栏目名称
        private const string TypeChannelIndex = "ChannelIndex";			                            //栏目索引
        private const string TypeTemplateName = "TemplateName";			                            //模板名称
        private const string TypTemplateType = "TemplateType";			                            //模板类型
        private const string TypeTopLevel = "TopLevel";			                                    //栏目级别
        private const string TypeUpChannel = "UpChannel";			                                //上级栏目
        private const string TypeUpChannelOrSelf = "UpChannelOrSelf";			                    //当前栏目或上级栏目
        private const string TypeSelfChannel = "SelfChannel";			                            //当前栏目
        private const string TypeGroupChannel = "GroupChannel";			                            //栏目组名称
        private const string TypeGroupContent = "GroupContent";			                            //内容组名称
        private const string TypeAddDate = "AddDate";			                                    //添加时间
        private const string TypeLastEditDate = "LastEditDate";			                            //最后编辑时间（仅用于判断内容）
        private const string TypeItemIndex = "ItemIndex";			                                //当前项序号
        private const string TypeOddItem = "OddItem";			                                    //奇数项

        public static SortedList<string, string> TypeList => new SortedList<string, string>
        {
            {TypeIsUserLoggin, "用户是否已登录"},
            {TypeIsAdministratorLoggin, "管理员是否已登录"},
            {TypeIsUserOrAdministratorLoggin, "用户或管理员是否已登录"},
            {TypeChannelName, "栏目名称"},
            {TypeChannelIndex, "栏目索引"},
            {TypeTemplateName, "模板名称"},
            {TypTemplateType, "模板类型"},
            {TypeTopLevel, "栏目级别"},
            {TypeUpChannel, "上级栏目"},
            {TypeUpChannelOrSelf, "当前栏目或上级栏目"},
            {TypeSelfChannel, "当前栏目"},
            {TypeGroupChannel, "栏目组名称"},
            {TypeGroupContent, "内容组名称"},
            {TypeAddDate, "添加时间"},
            {TypeLastEditDate, "最后编辑时间（仅用于判断内容）"},
            {TypeItemIndex, "当前项序号"},
            {TypeOddItem, "奇数项"}
        };

        public const string OperateEmpty = "Empty";
        public const string OperateNotEmpty = "NotEmpty";			                                //值不为空
        public const string OperateEquals = "Equals";			                                    //值等于
        public const string OperateNotEquals = "NotEquals";			                                //值不等于
        public const string OperateGreatThan = "GreatThan";			                                //值大于
        public const string OperateLessThan = "LessThan";			                                //值小于
        public const string OperateIn = "In";			                                            //值属于
        public const string OperateNotIn = "NotIn";                                                 //值不属于

        public static SortedList<string, string> OperateList => new SortedList<string, string>
        {
            {OperateEmpty, "值为空"},
            {OperateNotEmpty, "值不为空"},
            {OperateEquals, "值等于"},
            {OperateNotEquals, "值不等于"},
            {OperateGreatThan, "值大于"},
            {OperateLessThan, "值小于"},
            {OperateIn, "值属于"},
            {OperateNotIn, "值不属于"}
        };


        internal static async Task<object> ParseAsync(PageInfo pageInfo, ContextInfo contextInfo)
        {
            var testTypeStr = string.Empty;
            var testOperate = string.Empty;
            var testValue = string.Empty;
            var onBeforeSend = string.Empty;
            var onSuccess = string.Empty;
            var onComplete = string.Empty;
            var onError = string.Empty;

            foreach (var name in contextInfo.Attributes.AllKeys)
            {
                var value = contextInfo.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, Type) || StringUtils.EqualsIgnoreCase(name, "testType"))
                {
                    testTypeStr = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, Op) || StringUtils.EqualsIgnoreCase(name, "operate") || StringUtils.EqualsIgnoreCase(name, "testOperate"))
                {
                    testOperate = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, Value) || StringUtils.EqualsIgnoreCase(name, "testValue"))
                {
                    testValue = await StlEntityParser.ReplaceStlEntitiesForAttributeValueAsync(value, pageInfo, contextInfo);
                    if (string.IsNullOrEmpty(testOperate))
                    {
                        testOperate = OperateEquals;
                    }
                }
                else if (StringUtils.EqualsIgnoreCase(name, Context))
                {
                    contextInfo.ContextType = TranslateUtils.ToEnum(value, ContextType.Undefined);
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

            if (string.IsNullOrEmpty(testOperate))
            {
                testOperate = OperateNotEmpty;
            }

            return await ParseImplAsync(pageInfo, contextInfo, testTypeStr, testOperate, testValue, onBeforeSend, onSuccess, onComplete, onError);
        }

        private static async Task<string> ParseImplAsync(PageInfo pageInfo, ContextInfo contextInfo, string testType, string testOperate, string testValue, string onBeforeSend, string onSuccess, string onComplete, string onError)
        {
            string loading;
            string yes;
            string no;

            StlParserUtility.GetLoadingYesNo(contextInfo.InnerHtml, out loading, out yes, out no);

            if (StringUtils.EqualsIgnoreCase(testType, TypeIsUserLoggin) ||
                StringUtils.EqualsIgnoreCase(testType, TypeIsAdministratorLoggin) ||
                StringUtils.EqualsIgnoreCase(testType, TypeIsUserOrAdministratorLoggin))
            {
                return await ParseDynamicAsync(pageInfo, contextInfo, testType, testValue, testOperate, loading,
                    yes, no, onBeforeSend, onSuccess, onComplete, onError);
            }

            var isSuccess = false;
            if (StringUtils.EqualsIgnoreCase(testType, TypeChannelName))
            {
                var channelName = await DataProvider.ChannelRepository.GetChannelNameAsync(pageInfo.SiteId, contextInfo.ChannelId);
                isSuccess = TestTypeValue(testOperate, testValue, channelName);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeChannelIndex))
            {
                var channelIndex = await DataProvider.ChannelRepository.GetIndexNameAsync(pageInfo.SiteId, contextInfo.ChannelId);
                isSuccess = TestTypeValue(testOperate, testValue, channelIndex);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeTemplateName))
            {
                isSuccess = TestTypeValue(testOperate, testValue, pageInfo.Template.TemplateName);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypTemplateType))
            {
                isSuccess = TestTypeValue(testOperate, testValue, pageInfo.Template.TemplateType.GetValue());
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeTopLevel))
            {
                var topLevel = await DataProvider.ChannelRepository.GetTopLevelAsync(pageInfo.SiteId, contextInfo.ChannelId);
                isSuccess = IsNumber(topLevel, testOperate, testValue);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeUpChannel))
            {
                isSuccess = await TestTypeUpChannelAsync(pageInfo, contextInfo, testOperate, testValue);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeUpChannelOrSelf))
            {
                isSuccess = await TestTypeUpChannelOrSelfAsync(pageInfo, contextInfo, testOperate, testValue);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeSelfChannel))
            {
                isSuccess = pageInfo.PageChannelId == contextInfo.ChannelId;
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeGroupChannel))
            {
                var channel = await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId);
                isSuccess = TestTypeValues(testOperate, testValue, channel.GroupNames);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeGroupContent))
            {
                if (contextInfo.ContextType == ContextType.Content)
                {
                    var content = await contextInfo.GetContentAsync();
                    if (content != null)
                    {
                        var groupContents = content.GroupNames;
                        isSuccess = TestTypeValues(testOperate, testValue, groupContents);
                    }
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeAddDate))
            {
                var addDate = await GetAddDateByContextAsync(pageInfo, contextInfo);
                isSuccess = IsDateTime(addDate, testOperate, testValue);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeLastEditDate))
            {
                var lastEditDate = await GetLastEditDateByContextAsync(contextInfo);
                isSuccess = IsDateTime(lastEditDate, testOperate, testValue);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeItemIndex))
            {
                var itemIndex = StlParserUtility.GetItemIndex(contextInfo);
                isSuccess = IsNumber(itemIndex, testOperate, testValue);
            }
            else if (StringUtils.EqualsIgnoreCase(testType, TypeOddItem))
            {
                var itemIndex = StlParserUtility.GetItemIndex(contextInfo);
                isSuccess = itemIndex % 2 == 1;
            }
            else
            {
                isSuccess = await TestTypeDefaultAsync(pageInfo, contextInfo, testType, testOperate, testValue);
            }

            var parsedContent = isSuccess ? yes : no;

            if (string.IsNullOrEmpty(parsedContent)) return string.Empty;

            var innerBuilder = new StringBuilder(parsedContent);
            await StlParserManager.ParseInnerContentAsync(innerBuilder, pageInfo, contextInfo);

            parsedContent = innerBuilder.ToString();

            return parsedContent;
        }

        private static async Task<bool> TestTypeDefaultAsync(PageInfo pageInfo, ContextInfo contextInfo, string testType, string testOperate,
            string testValue)
        {
            var isSuccess = false;

            var theValue = await GetAttributeValueByContextAsync(pageInfo, contextInfo, testType);

            if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotEmpty))
            {
                if (!string.IsNullOrEmpty(theValue))
                {
                    isSuccess = true;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateEmpty))
            {
                if (string.IsNullOrEmpty(theValue))
                {
                    isSuccess = true;
                }
            }
            else
            {
                if (StringUtils.IsDateTime(theValue))
                {
                    var dateTime = TranslateUtils.ToDateTime(theValue);
                    isSuccess = IsDateTime(dateTime, testOperate, testValue);
                }
                else if (StringUtils.IsNumber(theValue))
                {
                    var number = TranslateUtils.ToInt(theValue);
                    isSuccess = IsNumber(number, testOperate, testValue);
                }
                else
                {
                    if (StringUtils.EqualsIgnoreCase(testOperate, OperateEquals))
                    {
                        if (StringUtils.EqualsIgnoreCase(theValue, testValue))
                        {
                            isSuccess = true;
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotEquals))
                    {
                        if (!StringUtils.EqualsIgnoreCase(theValue, testValue))
                        {
                            isSuccess = true;
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(testOperate, OperateGreatThan))
                    {
                        if (StringUtils.Contains(theValue, "-"))
                        {
                            if (TranslateUtils.ToDateTime(theValue) > TranslateUtils.ToDateTime(testValue))
                            {
                                isSuccess = true;
                            }
                        }
                        else
                        {
                            if (TranslateUtils.ToInt(theValue) > TranslateUtils.ToInt(testValue))
                            {
                                isSuccess = true;
                            }
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(testOperate, OperateLessThan))
                    {
                        if (StringUtils.Contains(theValue, "-"))
                        {
                            if (TranslateUtils.ToDateTime(theValue) < TranslateUtils.ToDateTime(testValue))
                            {
                                isSuccess = true;
                            }
                        }
                        else
                        {
                            if (TranslateUtils.ToInt(theValue) < TranslateUtils.ToInt(testValue))
                            {
                                isSuccess = true;
                            }
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(testOperate, OperateIn))
                    {
                        var stringList = Utilities.GetStringList(testValue);

                        foreach (var str in stringList)
                        {
                            if (StringUtils.EndsWithIgnoreCase(str, "*"))
                            {
                                var theStr = str.Substring(0, str.Length - 1);
                                if (StringUtils.StartsWithIgnoreCase(theValue, theStr))
                                {
                                    isSuccess = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (StringUtils.EqualsIgnoreCase(theValue, str))
                                {
                                    isSuccess = true;
                                    break;
                                }
                            }
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotIn))
                    {
                        var stringList = Utilities.GetStringList(testValue);

                        var isIn = false;
                        foreach (var str in stringList)
                        {
                            if (StringUtils.EndsWithIgnoreCase(str, "*"))
                            {
                                var theStr = str.Substring(0, str.Length - 1);
                                if (StringUtils.StartsWithIgnoreCase(theValue, theStr))
                                {
                                    isIn = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (StringUtils.EqualsIgnoreCase(theValue, str))
                                {
                                    isIn = true;
                                    break;
                                }
                            }
                        }
                        if (!isIn)
                        {
                            isSuccess = true;
                        }
                    }
                }
            }
            
            return isSuccess;
        }

        private static async Task<string> ParseDynamicAsync(PageInfo pageInfo, ContextInfo contextInfo, string testType, string testValue, string testOperate, string loading, string yes, string no, string onBeforeSend, string onSuccess, string onComplete, string onError)
        {
            if (string.IsNullOrEmpty(yes) && string.IsNullOrEmpty(no))
            {
                return string.Empty;
            }

            await pageInfo.AddPageBodyCodeIfNotExistsAsync(PageInfo.Const.StlClient);
            var ajaxDivId = StlParserUtility.GetAjaxDivId(pageInfo.UniqueId);

            //运行解析以便为页面生成所需JS引用
            if (!string.IsNullOrEmpty(yes))
            {
                await StlParserManager.ParseInnerContentAsync(new StringBuilder(yes), pageInfo, contextInfo);
            }
            if (!string.IsNullOrEmpty(no))
            {
                await StlParserManager.ParseInnerContentAsync(new StringBuilder(no), pageInfo, contextInfo);
            }

            var dynamicInfo = new DynamicInfo
            {
                ElementName = ElementName,
                SiteId = pageInfo.SiteId,
                ChannelId = contextInfo.ChannelId,
                ContentId = contextInfo.ContentId,
                TemplateId = pageInfo.Template.Id,
                AjaxDivId = ajaxDivId,
                LoadingTemplate = loading,
                SuccessTemplate = yes,
                FailureTemplate = no,
                OnBeforeSend = onBeforeSend,
                OnSuccess = onSuccess,
                OnComplete = onComplete,
                OnError = onError
            };
            var ifInfo = new DynamicInfo.IfInfo
            {
                Type = testType,
                Op = testOperate,
                Value = testValue
            };
            dynamicInfo.ElementValues = TranslateUtils.JsonSerialize(ifInfo);

            return dynamicInfo.GetScript(ApiRouteActionsIf.GetUrl(pageInfo.ApiUrl), true);
        }

        private static bool TestTypeValues(string testOperate, string testValue, List<string> actualValues)
        {
            var isSuccess = false;

            if (StringUtils.EqualsIgnoreCase(testOperate, OperateEquals) ||
                StringUtils.EqualsIgnoreCase(testOperate, OperateIn))
            {
                var stringList = Utilities.GetStringList(testValue);

                foreach (var str in stringList)
                {
                    if (!actualValues.Contains(str)) continue;
                    isSuccess = true;
                    break;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotEquals) ||
                     StringUtils.EqualsIgnoreCase(testOperate, OperateNotIn))
            {
                var stringList = Utilities.GetStringList(testValue);

                var isIn = false;
                foreach (var str in stringList)
                {
                    if (!actualValues.Contains(str)) continue;
                    isIn = true;
                    break;
                }
                if (!isIn)
                {
                    isSuccess = true;
                }
            }
            return isSuccess;
        }

        private static async Task<bool> TestTypeUpChannelOrSelfAsync(PageInfo pageInfo, ContextInfo contextInfo, string testOperate,
            string testValue)
        {
            var isSuccess = false;

            if (StringUtils.EqualsIgnoreCase(testOperate, OperateIn))
            {
                var channelIndexes = Utilities.GetStringList(testValue);
                var isIn = false;
                foreach (var channelIndex in channelIndexes)
                {
                    //var parentId = DataProvider.ChannelRepository.GetIdByIndexName(pageInfo.SiteId, channelIndex);
                    var parentId = await DataProvider.ChannelRepository.GetChannelIdByIndexNameAsync(pageInfo.SiteId, channelIndex);
                    if (!await DataProvider.ChannelRepository.IsAncestorOrSelfAsync(pageInfo.SiteId, parentId, pageInfo.PageChannelId)) continue;
                    isIn = true;
                    break;
                }
                if (isIn)
                {
                    isSuccess = true;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotIn))
            {
                var channelIndexes = Utilities.GetStringList(testValue);
                var isIn = false;
                foreach (var channelIndex in channelIndexes)
                {
                    //var parentId = DataProvider.ChannelRepository.GetIdByIndexName(pageInfo.SiteId, channelIndex);
                    int parentId = await DataProvider.ChannelRepository.GetChannelIdByIndexNameAsync(pageInfo.SiteId, channelIndex);
                    if (await DataProvider.ChannelRepository.IsAncestorOrSelfAsync(pageInfo.SiteId, parentId, pageInfo.PageChannelId))
                    {
                        isIn = true;
                        break;
                    }
                }
                if (!isIn)
                {
                    isSuccess = true;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(testValue))
                {
                    if (await DataProvider.ChannelRepository.IsAncestorOrSelfAsync(pageInfo.SiteId, contextInfo.ChannelId, pageInfo.PageChannelId))
                    {
                        isSuccess = true;
                    }
                }
                else
                {
                    var channelIndexes = Utilities.GetStringList(testValue);
                    foreach (var channelIndex in channelIndexes)
                    {
                        //var parentId = DataProvider.ChannelRepository.GetIdByIndexName(pageInfo.SiteId, channelIndex);
                        var parentId = await DataProvider.ChannelRepository.GetChannelIdByIndexNameAsync(pageInfo.SiteId, channelIndex);
                        if (await DataProvider.ChannelRepository.IsAncestorOrSelfAsync(pageInfo.SiteId, parentId, pageInfo.PageChannelId))
                        {
                            isSuccess = true;
                            break;
                        }
                    }
                }
            }
            return isSuccess;
        }

        private static async Task<bool> TestTypeUpChannelAsync(PageInfo pageInfo, ContextInfo contextInfo, string testOperate, string testValue)
        {
            var isSuccess = false;

            if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotIn))
            {
                var channelIndexes = Utilities.GetStringList(testValue);
                var isIn = false;
                foreach (var channelIndex in channelIndexes)
                {
                    //var parentId = DataProvider.ChannelRepository.GetIdByIndexName(pageInfo.SiteId, channelIndex);
                    var parentId = await DataProvider.ChannelRepository.GetChannelIdByIndexNameAsync(pageInfo.SiteId, channelIndex);
                    if (parentId != pageInfo.PageChannelId &&
                        await DataProvider.ChannelRepository.IsAncestorOrSelfAsync(pageInfo.SiteId, parentId, pageInfo.PageChannelId))
                    {
                        isIn = true;
                        break;
                    }
                }
                if (!isIn)
                {
                    isSuccess = true;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(testValue))
                {
                    if (contextInfo.ChannelId != pageInfo.PageChannelId &&
                        await DataProvider.ChannelRepository.IsAncestorOrSelfAsync(pageInfo.SiteId, contextInfo.ChannelId, pageInfo.PageChannelId))
                    {
                        isSuccess = true;
                    }
                }
                else
                {
                    var channelIndexes = Utilities.GetStringList(testValue);
                    foreach (var channelIndex in channelIndexes)
                    {
                        //var parentId = DataProvider.ChannelRepository.GetIdByIndexName(pageInfo.SiteId, channelIndex);
                        var parentId = await DataProvider.ChannelRepository.GetChannelIdByIndexNameAsync(pageInfo.SiteId, channelIndex);
                        if (parentId != pageInfo.PageChannelId &&
                            await DataProvider.ChannelRepository.IsAncestorOrSelfAsync(pageInfo.SiteId, parentId, pageInfo.PageChannelId))
                        {
                            isSuccess = true;
                            break;
                        }
                    }
                }
            }
            return isSuccess;
        }

        private static bool TestTypeValue(string testOperate, string testValue, string actualValue)
        {
            var isSuccess = false;
            if (StringUtils.EqualsIgnoreCase(testOperate, OperateEquals))
            {
                if (StringUtils.EndsWithIgnoreCase(testValue, "*"))
                {
                    var theStr = testValue.Substring(0, testValue.Length - 1);
                    if (StringUtils.StartsWithIgnoreCase(actualValue, theStr))
                    {
                        isSuccess = true;
                    }
                }
                else
                {
                    if (StringUtils.EqualsIgnoreCase(actualValue, testValue))
                    {
                        isSuccess = true;
                    }
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotEquals))
            {
                if (!StringUtils.EqualsIgnoreCase(actualValue, testValue))
                {
                    isSuccess = true;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateIn))
            {
                var stringList = Utilities.GetStringList(testValue);

                foreach (var str in stringList)
                {
                    if (StringUtils.EndsWithIgnoreCase(str, "*"))
                    {
                        var theStr = str.Substring(0, str.Length - 1);
                        if (StringUtils.StartsWithIgnoreCase(actualValue, theStr))
                        {
                            isSuccess = true;
                            break;
                        }
                    }
                    else
                    {
                        if (StringUtils.EqualsIgnoreCase(actualValue, str))
                        {
                            isSuccess = true;
                            break;
                        }
                    }
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotIn))
            {
                var stringList = Utilities.GetStringList(testValue);

                var isIn = false;
                foreach (var str in stringList)
                {
                    if (StringUtils.EndsWithIgnoreCase(str, "*"))
                    {
                        var theStr = str.Substring(0, str.Length - 1);
                        if (StringUtils.StartsWithIgnoreCase(actualValue, theStr))
                        {
                            isIn = true;
                            break;
                        }
                    }
                    else
                    {
                        if (StringUtils.EqualsIgnoreCase(actualValue, str))
                        {
                            isIn = true;
                            break;
                        }
                    }
                }
                if (!isIn)
                {
                    isSuccess = true;
                }
            }
            return isSuccess;
        }

        private static async Task<string> GetAttributeValueByContextAsync(PageInfo pageInfo, ContextInfo contextInfo, string testTypeStr)
        {
            string theValue = null;
            if (contextInfo.ContextType == ContextType.Content)
            {
                theValue = await GetValueFromContentAsync(pageInfo, contextInfo, testTypeStr);
            }
            else if (contextInfo.ContextType == ContextType.Channel)
            {
                theValue = await GetValueFromChannelAsync(pageInfo, contextInfo, testTypeStr);
            }
            else if (contextInfo.ContextType == ContextType.SqlContent)
            {
                contextInfo.ItemContainer.SqlItem.Value.TryGetValue(testTypeStr, out var obj);
                if (obj != null)
                {
                    theValue = obj.ToString();
                }
            }
            else if (contextInfo.ContextType == ContextType.Site)
            {
                var site = contextInfo.ItemContainer.SiteItem.Value;
                if (site != null)
                {
                    theValue = site.Get<string>(testTypeStr);
                }
            }
            else
            {
                if (contextInfo.ItemContainer != null)
                {
                    //else if (contextInfo.ItemContainer.InputItem != null)
                    //{
                    //    theValue = DataBinder.Eval(contextInfo.ItemContainer.InputItem.DataItem, testTypeStr, "{0}");
                    //}
                    //else if (contextInfo.ItemContainer.ContentItem != null)
                    //{
                    //    theValue = DataBinder.Eval(contextInfo.ItemContainer.ContentItem.DataItem, testTypeStr, "{0}");
                    //}
                    //else if (contextInfo.ItemContainer.ChannelItem != null)
                    //{
                    //    theValue = DataBinder.Eval(contextInfo.ItemContainer.ChannelItem.DataItem, testTypeStr, "{0}");
                    //}
                    //if (contextInfo.ItemContainer.SqlItem != null)
                    //{
                    //    theValue = DataBinder.Eval(contextInfo.ItemContainer.SqlItem.DataItem, testTypeStr, "{0}");
                    //}
                }
                else if (contextInfo.ContentId != 0)//获取内容
                {
                    theValue = await GetValueFromContentAsync(pageInfo, contextInfo, testTypeStr);
                }
                else if (contextInfo.ChannelId != 0)//获取栏目
                {
                    theValue = await GetValueFromChannelAsync(pageInfo, contextInfo, testTypeStr);
                }
            }

            return theValue ?? string.Empty;
        }

        private static async Task<string> GetValueFromChannelAsync(PageInfo pageInfo, ContextInfo contextInfo, string testTypeStr)
        {
            string theValue;

            var channel = await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId);

            if (StringUtils.EqualsIgnoreCase(nameof(Channel.AddDate), testTypeStr))
            {
                theValue = DateUtils.GetDateAndTimeString(channel.AddDate);
            }
            else if (StringUtils.EqualsIgnoreCase(StlParserUtility.Title, testTypeStr) || StringUtils.EqualsIgnoreCase(nameof(Channel.ChannelName), testTypeStr))
            {
                theValue = channel.ChannelName;
            }
            else if (StringUtils.EqualsIgnoreCase(nameof(Channel.ImageUrl), testTypeStr))
            {
                theValue = channel.ImageUrl;
            }
            else if (StringUtils.EqualsIgnoreCase(nameof(Channel.Content), testTypeStr))
            {
                theValue = channel.Content;
            }
            else if (StringUtils.EqualsIgnoreCase(StlParserUtility.CountOfChannels, testTypeStr))
            {
                theValue = channel.ChildrenCount.ToString();
            }
            else if (StringUtils.EqualsIgnoreCase(StlParserUtility.CountOfContents, testTypeStr))
            {
                var count = await DataProvider.ContentRepository.GetCountAsync(pageInfo.Site, channel);
                theValue = count.ToString();
            }
            else if (StringUtils.EqualsIgnoreCase(StlParserUtility.CountOfImageContents, testTypeStr))
            {
                var count = await DataProvider.ContentRepository.GetCountCheckedImageAsync(pageInfo.Site, channel);
                theValue = count.ToString();
            }
            else if (StringUtils.EqualsIgnoreCase(nameof(Channel.LinkUrl), testTypeStr))
            {
                theValue = channel.LinkUrl;
            }
            else
            {
                theValue = channel.Get<string>(testTypeStr);
            }
            return theValue;
        }

        private static async Task<string> GetValueFromContentAsync(PageInfo pageInfo, ContextInfo contextInfo, string testTypeStr)
        {
            var theValue = string.Empty;

            var contentInfo = await contextInfo.GetContentAsync();

            if (contentInfo != null)
            {
                theValue = contentInfo.Get<string>(testTypeStr);
            }

            return theValue;
        }

        private static async Task<DateTime> GetAddDateByContextAsync(PageInfo pageInfo, ContextInfo contextInfo)
        {
            var addDate = Constants.SqlMinValue;

            var contentInfo = await contextInfo.GetContentAsync();

            if (contextInfo.ContextType == ContextType.Content)
            {
                if (contentInfo.AddDate.HasValue)
                {
                    addDate = contentInfo.AddDate.Value;
                }
            }
            else if (contextInfo.ContextType == ContextType.Channel)
            {
                var channel = await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId);
                if (channel.AddDate.HasValue)
                {
                    addDate = channel.AddDate.Value;
                }
            }
            else
            {
                if (contextInfo.ItemContainer != null)
                {
                    //else if (contextInfo.ItemContainer.InputItem != null)
                    //{
                    //    addDate = (DateTime)DataBinder.Eval(contextInfo.ItemContainer.InputItem.DataItem, InputContentAttribute.AddDate);
                    //}
                    //else if (contextInfo.ItemContainer.ContentItem != null)
                    //{
                    //    addDate = (DateTime)DataBinder.Eval(contextInfo.ItemContainer.ContentItem.DataItem, ContentAttribute.AddDate);
                    //}
                    //else if (contextInfo.ItemContainer.ChannelItem != null)
                    //{
                    //    addDate = (DateTime)DataBinder.Eval(contextInfo.ItemContainer.ChannelItem.DataItem, NodeAttribute.AddDate);
                    //}
                    //if (contextInfo.ItemContainer.SqlItem != null)
                    //{
                    //    addDate = (DateTime)DataBinder.Eval(contextInfo.ItemContainer.SqlItem.DataItem, "AddDate");
                    //}
                }
                else if (contextInfo.ContentId != 0)//获取内容
                {
                    if (contentInfo.AddDate.HasValue)
                    {
                        addDate = contentInfo.AddDate.Value;
                    }
                }
                else if (contextInfo.ChannelId != 0)//获取栏目
                {
                    var channel = await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId);
                    if (channel.AddDate.HasValue)
                    {
                        addDate = channel.AddDate.Value;
                    }
                }
            }

            return addDate;
        }

        private static async Task<DateTime> GetLastEditDateByContextAsync(ContextInfo contextInfo)
        {
            var lastEditDate = Constants.SqlMinValue;

            var contentInfo = await contextInfo.GetContentAsync();

            if (contextInfo.ContextType == ContextType.Content)
            {
                if (contentInfo.LastEditDate.HasValue)
                {
                    lastEditDate = contentInfo.LastEditDate.Value;
                }
            }

            return lastEditDate;
        }

        private static bool IsNumber(int number, string testOperate, string testValue)
        {
            var isSuccess = false;
            
            if (StringUtils.EqualsIgnoreCase(testOperate, OperateEquals))
            {
                if (number == TranslateUtils.ToInt(testValue))
                {
                    isSuccess = true;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotEquals))
            {
                if (number != TranslateUtils.ToInt(testValue))
                {
                    isSuccess = true;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateGreatThan))
            {
                if (number > TranslateUtils.ToInt(testValue))
                {
                    isSuccess = true;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateLessThan))
            {
                if (number < TranslateUtils.ToInt(testValue))
                {
                    isSuccess = true;
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateIn))
            {
                var intArrayList = Utilities.GetIntList(testValue);
                foreach (int i in intArrayList)
                {
                    if (i == number)
                    {
                        isSuccess = true;
                        break;
                    }
                }
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotIn))
            {
                var intArrayList = Utilities.GetIntList(testValue);
                var isIn = false;
                foreach (int i in intArrayList)
                {
                    if (i == number)
                    {
                        isIn = true;
                        break;
                    }
                }
                if (!isIn)
                {
                    isSuccess = true;
                }
            }
            return isSuccess;
        }

        private static bool IsDateTime(DateTime dateTime, string testOperate, string testValue)
        {
            var isSuccess = false;

            DateTime dateTimeToTest;

            if (StringUtils.EqualsIgnoreCase("now", testValue))
            {
                dateTimeToTest = DateTime.Now;
            }
            else if (DateUtils.IsSince(testValue))
            {
                var hours = DateUtils.GetSinceHours(testValue);
                dateTimeToTest = DateTime.Now.AddHours(-hours);
            }
            else
            {
                dateTimeToTest = TranslateUtils.ToDateTime(testValue);
            }

            if (StringUtils.EqualsIgnoreCase(testOperate, OperateEquals) || StringUtils.EqualsIgnoreCase(testOperate, OperateIn))
            {
                isSuccess = dateTime.Date == dateTimeToTest.Date;
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateGreatThan))
            {
                isSuccess = dateTime > dateTimeToTest;
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateLessThan))
            {
                isSuccess = dateTime < dateTimeToTest;
            }
            else if (StringUtils.EqualsIgnoreCase(testOperate, OperateNotEquals) || StringUtils.EqualsIgnoreCase(testOperate, OperateNotIn))
            {
                isSuccess = dateTime.Date != dateTimeToTest.Date;
            }

            return isSuccess;
        }
    }
}

