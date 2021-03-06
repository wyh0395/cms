﻿using System.Collections.Generic;
using Datory;
using Newtonsoft.Json;
using SS.CMS.Abstractions;
using SS.CMS.Framework;

namespace SS.CMS.Cli.Updater.Tables
{
    public partial class TableTag
    {
        [JsonProperty("tagID")]
        public long TagId { get; set; }

        [JsonProperty("productID")]
        public string ProductId { get; set; }

        [JsonProperty("publishmentSystemID")]
        public long PublishmentSystemId { get; set; }

        [JsonProperty("contentIDCollection")]
        public string ContentIdCollection { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("useNum")]
        public long UseNum { get; set; }
    }

    public partial class TableTag
    {
        public const string OldTableName = "bairong_Tags";

        public static ConvertInfo Converter => new ConvertInfo
        {
            NewTableName = NewTableName,
            NewColumns = NewColumns,
            ConvertKeyDict = ConvertKeyDict,
            ConvertValueDict = ConvertValueDict
        };

        private static readonly string NewTableName = DataProvider.ContentTagRepository.TableName;

        private static readonly List<TableColumn> NewColumns = DataProvider.ContentTagRepository.TableColumns;

        private static readonly Dictionary<string, string> ConvertKeyDict =
            new Dictionary<string, string>
            {
                {nameof(ContentTag.Id), nameof(TagId)},
                {nameof(ContentTag.SiteId), nameof(PublishmentSystemId)}
            };

        private static readonly Dictionary<string, string> ConvertValueDict = null;
    }
}
