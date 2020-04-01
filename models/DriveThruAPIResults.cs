namespace ModuleCrier2.Models  
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class DriveThruAPIResults
    {
        [JsonProperty("results")]
        public NewDriveThruListing[] Results { get; set; }
    }

    public partial class NewDriveThruListing 
    {
        [JsonProperty("products_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long ProductsId { get; set; }

        [JsonProperty("products_number")]
        public string ProductsNumber { get; set; }

        [JsonProperty("products_name")]
        public string ProductsName { get; set; }

        [JsonProperty("products_image")]
        public Uri ProductsImage { get; set; }

        [JsonProperty("whats_new_price")]
        public string WhatsNewPrice { get; set; }

        [JsonProperty("lc_text")]
        public string LcText { get; set; }

        [JsonProperty("products_is_pay_what_you_want")]
        public object ProductsIsPayWhatYouWant { get; set; }

        [JsonProperty("products_avg_rating")]
        public string ProductsAvgRating { get; set; }

        [JsonProperty("test_ab_recommendation")]
        public object TestAbRecommendation { get; set; }
    }

    public partial class DriveThruAPIResults
    {
        public static DriveThruAPIResults FromJson(string json) => JsonConvert.DeserializeObject<DriveThruAPIResults>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this DriveThruAPIResults self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}
