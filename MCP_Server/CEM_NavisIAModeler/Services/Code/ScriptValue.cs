// Cemengal: turns what a send_code_to_navisworks script returned into JSON the agent can read
// (docs/decisions/0010). A plain serializer would walk a returned ModelItem's properties, Descendants
// included, through the whole model on the UI thread; here Navisworks objects become text, and depth
// and size are bounded.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using Autodesk.Navisworks.Api;

namespace CEM_NavisIAModeler.Services.Code
{
    public static class ScriptValue
    {
        private const int MaxDepth = 6;
        private const int MaxItemsPerList = 1000;
        private const int MaxNodes = 20000;

        /// <summary>The value as JSON, bounded; Navisworks and other library objects as text.</summary>
        public static string ToJson(object value)
        {
            var budget = new int[] { MaxNodes };
            var plain = ToPlain(value, 0, budget);
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(plain);
        }

        private static object ToPlain(object value, int depth, int[] budget)
        {
            if (value == null) return null;
            if (--budget[0] < 0) return "...";

            switch (value)
            {
                case string s: return s;
                case bool _: return value;
                case char c: return c.ToString();
                case Enum e: return e.ToString();
                case Guid g: return g.ToString("D");
                case DateTime d: return d.ToString("o", CultureInfo.InvariantCulture);
                case ModelItem item: return Describe(item);
            }
            if (value.GetType().IsPrimitive || value is decimal) return value;
            if (depth >= MaxDepth) return value.ToString();

            if (value is IDictionary dictionary)
            {
                var map = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (map.Count >= MaxItemsPerList) { map["..."] = "more entries"; break; }
                    map[Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? ""] = ToPlain(entry.Value, depth + 1, budget);
                }
                return map;
            }

            if (value is IEnumerable sequence)
            {
                var list = new List<object>();
                foreach (var element in sequence)
                {
                    if (list.Count >= MaxItemsPerList) { list.Add("... more items"); break; }
                    list.Add(ToPlain(element, depth + 1, budget));
                }
                return list;
            }

            // Anonymous objects and the script's own types are read property by property; library types,
            // the Navisworks API above all, are not walked.
            var type = value.GetType();
            var ns = type.Namespace ?? "";
            if (ns.StartsWith("Autodesk", StringComparison.Ordinal) || ns.StartsWith("System", StringComparison.Ordinal) || ns.StartsWith("Microsoft", StringComparison.Ordinal))
                return value.ToString();

            var properties = new Dictionary<string, object>();
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                object propertyValue;
                try { propertyValue = property.GetValue(value); }
                catch (Exception ex) { propertyValue = $"<{ex.GetType().Name}>"; }
                properties[property.Name] = ToPlain(propertyValue, depth + 1, budget);
            }
            return properties;
        }

        private static string Describe(ModelItem item)
        {
            try
            {
                var name = string.IsNullOrEmpty(item.DisplayName) ? item.ClassDisplayName : item.DisplayName;
                var guid = item.InstanceGuid;
                return guid == Guid.Empty ? $"{name} [{item.ClassDisplayName}]" : $"{name} [{item.ClassDisplayName}] {guid:D}";
            }
            catch (Exception)
            {
                return "ModelItem";
            }
        }
    }
}
