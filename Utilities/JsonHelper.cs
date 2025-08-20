using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Reckoner.Utilities
{
  using System;
  using System.Text.Json;
  using System.Text.Json.Serialization;

  public class DateOnlyJsonConverter : JsonConverter<DateTime?>
  {
    private const string Format = "yyyy-MM-dd";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType == JsonTokenType.Null)
      {
        return null;
      }

      var value = reader.GetString();
      return DateTime.TryParseExact(value, Format, null, System.Globalization.DateTimeStyles.None, out var result)
          ? result
          : throw new JsonException($"Invalid DateTime format. Expected format is {Format}.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
      if (value.HasValue)
      {
        writer.WriteStringValue(value.Value.ToString(Format));
      }
      else
      {
        writer.WriteNullValue();
      }
    }

    // Ensure a parameterless constructor exists
    public DateOnlyJsonConverter() { }
  }
}

