using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace wwoivre.azure.sandbox.tooling.helpers
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.GetCultureInfo("en-US")));
        }
    }
}
