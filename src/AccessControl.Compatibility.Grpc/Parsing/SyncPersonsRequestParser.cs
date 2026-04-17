using System;
using System.Collections.Generic;
using System.Globalization;
using ControlEntradaSalida.Application.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ControlEntradaSalida.Compatibility.Grpc.Parsing
{
    public static class SyncPersonsRequestParser
    {
        public static IReadOnlyList<PersonSyncCommandItem> Parse(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return Array.Empty<PersonSyncCommandItem>();
            }

            JToken root = JToken.Parse(payload);
            var items = new List<PersonSyncCommandItem>();

            foreach (JToken token in Enumerate(root))
            {
                items.Add(ParseItem(token));
            }

            return items;
        }

        private static IEnumerable<JToken> Enumerate(JToken root)
        {
            if (root == null)
            {
                yield break;
            }

            if (root.Type == JTokenType.Array)
            {
                foreach (JToken item in root)
                {
                    if (item != null)
                    {
                        yield return item;
                    }
                }

                yield break;
            }

            if (root.Type == JTokenType.Object)
            {
                foreach (string property in new[] { "people", "items", "records", "data" })
                {
                    if (root[property] is JArray array)
                    {
                        foreach (JToken item in array)
                        {
                            if (item != null)
                            {
                                yield return item;
                            }
                        }

                        yield break;
                    }
                }

                yield return root;
                yield break;
            }

            throw new JsonException("不支持的JSON结构。");
        }

        private static PersonSyncCommandItem ParseItem(JToken token)
        {
            string employeeId = ReadString(token, "employee_id", "employeeId", "employee_no", "employeeNo");
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("字段 employee_id 不能为空。");
            }

            string faceBase64 = ReadString(token, "face_image_base64", "faceImageBase64", "face_base64", "faceBase64", "face_image");
            return new PersonSyncCommandItem
            {
                EmployeeId = employeeId,
                FullName = ReadString(token, "name", "full_name", "fullName"),
                Gender = ReadString(token, "gender", "sex"),
                Enabled = ReadBool(token, "enabled", "active", "is_active") ?? true,
                ValidFrom = ReadDate(token, "valid_from", "validFrom"),
                ValidTo = ReadDate(token, "valid_to", "validTo"),
                FaceImageFormat = ReadString(token, "face_image_format", "faceImageFormat"),
                FaceImageBytes = string.IsNullOrWhiteSpace(faceBase64) ? null : ParseFaceBytes(faceBase64)
            };
        }

        private static string ReadString(JToken token, params string[] aliases)
        {
            foreach (string alias in aliases)
            {
                JToken valueToken = token?[alias];
                if (valueToken == null || valueToken.Type == JTokenType.Null)
                {
                    continue;
                }

                string value = valueToken.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static bool? ReadBool(JToken token, params string[] aliases)
        {
            foreach (string alias in aliases)
            {
                JToken valueToken = token?[alias];
                if (valueToken == null || valueToken.Type == JTokenType.Null)
                {
                    continue;
                }

                if (valueToken.Type == JTokenType.Boolean)
                {
                    return valueToken.Value<bool>();
                }

                if (bool.TryParse(valueToken.ToString(), out bool parsed))
                {
                    return parsed;
                }
            }

            return null;
        }

        private static DateTime? ReadDate(JToken token, params string[] aliases)
        {
            string value = ReadString(token, aliases);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime parsed))
            {
                return parsed;
            }

            throw new ArgumentException("时间字段格式不正确。");
        }

        private static byte[] ParseFaceBytes(string base64Value)
        {
            string normalized = base64Value.Trim();
            int commaIndex = normalized.IndexOf(',');
            if (commaIndex >= 0)
            {
                normalized = normalized.Substring(commaIndex + 1);
            }

            return Convert.FromBase64String(normalized);
        }
    }
}
