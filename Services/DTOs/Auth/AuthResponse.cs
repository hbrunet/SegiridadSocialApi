// <copyright file="AuthResponse.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeguridadSocialApi.Services.DTOs.Auth;

/// <summary>
/// Response completo de la API de autenticación externa.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets si la operación fue exitosa.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets el mensaje de respuesta.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets los datos de autenticación.
    /// </summary>
    [JsonPropertyName("data")]
    public AuthData? Data { get; set; }

    /// <summary>
    /// Gets or sets los detalles del error cuando la operación falla.
    /// Puede ser un string simple o un objeto ErrorDetails.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonConverter(typeof(FlexibleErrorConverter))]
    public ErrorDetails? Error { get; set; }

    /// <summary>
    /// Gets or sets el timestamp de la respuesta.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Detalles del error retornado por la API externa.
/// </summary>
public class ErrorDetails
{
    /// <summary>
    /// Gets or sets el código de error (ej: "01017").
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets el detalle técnico del error.
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// Converter personalizado para manejar el campo error que puede ser string u objeto.
/// </summary>
public class FlexibleErrorConverter : JsonConverter<ErrorDetails?>
{
    public override ErrorDetails? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Si es un string, crear ErrorDetails con el string como Detail
            var errorMessage = reader.GetString();
            return new ErrorDetails
            {
                Code = string.Empty,
                Detail = errorMessage ?? string.Empty
            };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Si es un objeto, deserializar normalmente
            return JsonSerializer.Deserialize<ErrorDetails>(ref reader, options);
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} for error field");
    }

    public override void Write(Utf8JsonWriter writer, ErrorDetails? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Escribir como objeto
        JsonSerializer.Serialize(writer, value, options);
    }
}
