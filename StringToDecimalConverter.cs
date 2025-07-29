using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StringToDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Se o token no JSON for do tipo String
        if (reader.TokenType == JsonTokenType.String)
        {
            string? stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null; // Retorna nulo se a string for vazia
            }
            
            // Tenta converter a string para decimal
            if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }
        }
        // Se o token já for um número
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }
        // Se o token for nulo
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // --- CORREÇÃO FINAL AQUI ---
        // Removida a tentativa de acessar a propriedade .Path, que não existe em Utf8JsonReader.
        throw new JsonException("O valor recebido não é uma string ou número que possa ser convertido para decimal?.");
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}