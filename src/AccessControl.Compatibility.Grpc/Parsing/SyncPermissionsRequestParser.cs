using System;
using System.Collections.Generic;
using System.Globalization;
using ControlEntradaSalida.Application.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ControlEntradaSalida.Compatibility.Grpc.Parsing
{
    public static class SyncPermissionsRequestParser
    {
        public static IReadOnlyList<PermissionUpdateCommandItem> Parse(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return Array.Empty<PermissionUpdateCommandItem>();
            }

            JToken root = JToken.Parse(payload);
            var items = new List<PermissionUpdateCommandItem>();

            if (root.Type == JTokenType.Array)
            {
                foreach (JToken item in root)
                {
                    AddItem(item, items);
                }
            }
            else if (root.Type == JTokenType.Object)
            {
                if (root["items"] is JArray itemsArray)
                {
                    foreach (JToken item in itemsArray)
                    {
                        AddItem(item, items);
                    }
                }
                else if (root["records"] is JArray recordsArray)
                {
                    foreach (JToken item in recordsArray)
                    {
                        AddItem(item, items);
                    }
                }
                else
                {
                    AddItem(root, items);
                }
            }
            else
            {
                throw new JsonException("不支持的JSON结构。");
            }

            return items;
        }

        private static void AddItem(JToken token, ICollection<PermissionUpdateCommandItem> items)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                return;
            }

            string employeeId = token.Value<string>("employee_id");
            JToken permissionToken = token["permission_code"];
            if (string.IsNullOrWhiteSpace(employeeId) || permissionToken == null)
            {
                throw new ArgumentException("缺少员工或权限字段。");
            }

            if (!int.TryParse(permissionToken.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int permissionCode))
            {
                throw new ArgumentException("permission_code 无效。");
            }

            items.Add(new PermissionUpdateCommandItem(employeeId, permissionCode));
        }
    }
}
