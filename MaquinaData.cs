using System;
using System.Text.Json.Serialization;

public class MaquinaData
{
    [JsonPropertyName("numeroSerial")]
    public string? NumeroSerial { get; set; }

    [JsonPropertyName("operador_id")]
    public int? OperadorId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    // --- APLIQUE O CONVERSOR AQUI ---
    [JsonPropertyName("velocidade")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <--- Adicione esta linha
    public decimal? Velocidade { get; set; }

    // --- E APLIQUE AQUI TAMBÉM ---
    [JsonPropertyName("producao")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <--- Adicione esta linha
    public decimal? Producao { get; set; }

    [JsonPropertyName("timestamp")]
    public string? TimestampRaw { get; set; }

    public string? CnpjEmpresa { get; set; }

    public int? MaquinaId { get; set; }

    [JsonIgnore]
    public DateTime? Timestamp
    {
        get
        {
            if (!string.IsNullOrEmpty(TimestampRaw) && DateTime.TryParseExact(
                TimestampRaw,
                "yyyy-MM-dd/HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedDateTime))
            {
                return parsedDateTime;
            }
            return null;
        }
    }
}