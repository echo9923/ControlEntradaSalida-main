using System;
using Newtonsoft.Json.Linq;

namespace ControlEntradaSalida.Compatibility.Grpc
{
    public static class PayloadMasker
    {
        public static string Mask(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return string.Empty;
            }

            try
            {
                JToken token = JToken.Parse(payload);
                MaskToken(token);
                return token.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch
            {
                return payload;
            }
        }

        private static void MaskToken(JToken token)
        {
            if (token == null)
            {
                return;
            }

            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty property in ((JObject)token).Properties())
                {
                    if (string.Equals(property.Name, "password", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(property.Name, "faceImageBase64", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(property.Name, "face_image_base64", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value = "***";
                        continue;
                    }

                    MaskToken(property.Value);
                }
            }

            if (token.Type == JTokenType.Array)
            {
                foreach (JToken child in (JArray)token)
                {
                    MaskToken(child);
                }
            }
        }
    }
}
