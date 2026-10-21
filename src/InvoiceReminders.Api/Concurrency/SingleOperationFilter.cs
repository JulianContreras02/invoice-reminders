namespace InvoiceReminders.Api.Concurrency;

/// <summary>
/// Si ya hay una operación en curso (por ejemplo, un doble clic en "Ejecutar"), responde 409
/// en lugar de lanzar un segundo lote. Entre instancias, la protección es el compare-and-set (ADR 0004).
/// </summary>
internal sealed class SingleOperationFilter(OperationGate gate) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!gate.TryEnter())
        {
            return TypedResults.Problem(
                title: "Operación en curso",
                detail: "Ya hay una ejecución o un reinicio en curso. Intente de nuevo en unos segundos.",
                statusCode: StatusCodes.Status409Conflict);
        }

        try
        {
            return await next(context);
        }
        finally
        {
            gate.Exit();
        }
    }
}
