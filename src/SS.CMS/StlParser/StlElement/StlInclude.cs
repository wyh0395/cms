﻿using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SS.CMS.Abstractions;
using SS.CMS;
using SS.CMS.StlParser.Model;
using SS.CMS.StlParser.Parsers;
using SS.CMS.StlParser.Utility;
using SS.CMS.Core;
using SS.CMS.Framework;

namespace SS.CMS.StlParser.StlElement
{
    [StlElement(Title = "包含文件", Description = "通过 stl:include 标签在模板中包含另一个文件，作为模板的一部分")]
    public class StlInclude
	{
		private StlInclude(){}
		public const string ElementName = "stl:include";

        [StlAttribute(Title = "文件路径")]
        private const string File = nameof(File);
        
        public static async Task<object> ParseAsync(PageInfo pageInfo, ContextInfo contextInfo)
		{
		    var file = string.Empty;
            var parameters = new Dictionary<string, string>();

            foreach (var name in contextInfo.Attributes.AllKeys)
            {
                var value = contextInfo.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, File))
                {
                    file = StringUtils.ReplaceIgnoreCase(value, "{Stl.SiteUrl}", "@");
                    file = await StlEntityParser.ReplaceStlEntitiesForAttributeValueAsync(file, pageInfo, contextInfo);
                    file = PageUtility.AddVirtualToUrl(file);
                }
                else
                {
                    parameters[name] = await StlEntityParser.ReplaceStlEntitiesForAttributeValueAsync(value, pageInfo, contextInfo);
                }
            }

            return await ParseImplAsync(pageInfo, contextInfo, file, parameters);
		}

        private static async Task<string> ParseImplAsync(PageInfo pageInfo, ContextInfo contextInfo, string file, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(file)) return string.Empty;

            var pageParameters = pageInfo.Parameters;
            pageInfo.Parameters = parameters;

            var content = await DataProvider.TemplateRepository.GetIncludeContentAsync(pageInfo.Site, file);
            var contentBuilder = new StringBuilder(content);
            await StlParserManager.ParseTemplateContentAsync(contentBuilder, pageInfo, contextInfo);
            var parsedContent = contentBuilder.ToString();

            pageInfo.Parameters = pageParameters;

            return parsedContent;
        }
	}
}
