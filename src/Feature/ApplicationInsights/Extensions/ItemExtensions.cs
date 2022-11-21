using Sitecore.Data;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeanOBrien.Feature.ApplicationInsights.Extensions
{
    public static class ItemExtensions
    {
        public static List<Item> GetLinkedItems(this Item item, string fieldName)
        {
            return GetLinkedItems(item.Database, item[fieldName]);
        }
        private static List<Item> GetLinkedItems(Database database, string fieldValue)
        {
            var items = new List<Item>();

            if (!string.IsNullOrEmpty(fieldValue))
            {
                var ids = fieldValue.Split(new char[] { '|' });

                foreach (var id in ids)
                {
                    var linkedItem = database.GetItem(new Sitecore.Data.ID(ParseId(id)));

                    if (linkedItem != null && !linkedItem.Publishing.NeverPublish)
                    {
                        items.Add(linkedItem);
                    }
                }
            }

            return items;
        }
        private static string ParseId(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (id.StartsWith("<image "))
                {
                    var regex = new Regex(@"{[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}}", RegexOptions.IgnoreCase);
                    var match = regex.Match(id);

                    if (match.Success)
                    {
                        id = match.Value;
                    }

                    return id;
                }
            }

            return id;
        }
    }
}
