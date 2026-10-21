using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InvoiceReminders.Infrastructure.Mongo;

// Contrato de datos en MongoDB (ADR 0005): colecciones y campos en español porque "estado"
// y sus valores son el contrato visible. El dominio no lleva atributos de BSON; la traducción
// ocurre solo en DocumentMappers.

[BsonIgnoreExtraElements]
internal sealed class CustomerDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("nombre")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("creadoEn")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
}

[BsonIgnoreExtraElements]
internal sealed class InvoiceDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("clienteId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("monto")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    [BsonElement("moneda")]
    public string Currency { get; set; } = "COP";

    [BsonElement("fechaEmision")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime IssuedOn { get; set; }

    [BsonElement("fechaVencimiento")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DueOn { get; set; }

    [BsonElement("estado")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("historialEstados")]
    public List<StatusChangeDocument> StatusHistory { get; set; } = [];

    [BsonElement("actualizadoEn")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}

internal sealed class StatusChangeDocument
{
    [BsonElement("de")]
    public string From { get; set; } = string.Empty;

    [BsonElement("a")]
    public string To { get; set; } = string.Empty;

    [BsonElement("fecha")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime At { get; set; }

    [BsonElement("motivo")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("ejecucionId")]
    [BsonIgnoreIfNull]
    public string? RunId { get; set; }

    [BsonElement("mensajeId")]
    [BsonIgnoreIfNull]
    public string? MessageId { get; set; }
}
