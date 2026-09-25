using System.Text.Json;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.Agents.Safety;
using FixFlow.Application.Agents.Tools;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Enums;

namespace FixFlow.Application.Agents;

public class BookingValidationAgent(IAiModelClient model, IAgentToolExecutor tools)
{
    public const AgentRole Role = AgentRole.BookingValidation;

    public async Task<(ValidationOutput Output, IReadOnlyList<string> ToolsUsed)> RunAsync(
        ValidationInput input,
        AgentToolContext context,
        int maxOutputChars,
        CancellationToken cancellationToken)
    {
        var used = new List<string>();
        var errors = new List<string>();

        async Task<JsonElement> Call(string name, object args)
        {
            var result = await tools.CallAsync(context, name, JsonSerializer.Serialize(args, AgentJson.Options), cancellationToken);
            used.Add(name);
            if (result.ErrorCode == "TOOL_TIMEOUT")
            {
                throw new AgentOutputException("TOOL_TIMEOUT", result.OutputJson);
            }

            if (!result.Ok)
            {
                errors.Add($"{name}: {result.ErrorCode}");
            }

            return JsonDocument.Parse(result.OutputJson).RootElement.Clone();
        }

        var request = await Call("GetRequest", new { requestId = input.RequestId });
        var quote = await Call("GetQuotation", new { quotationId = input.QuotationId });
        var expiry = await Call("CheckQuoteExpiry", new { quotationId = input.QuotationId });
        var ownership = await Call("CheckCustomerOwnership", new { requestId = input.RequestId, customerId = input.CustomerId });
        var relationship = await Call("CheckQuoteRequestRelationship", new { quotationId = input.QuotationId, requestId = input.RequestId });
        var approval = await Call("CheckApproval", new { workflowId = input.WorkflowId, customerId = input.CustomerId });
        var conflict = await Call("CheckBookingConflict", new { requestId = input.RequestId });
        var availability = await Call("CheckTechnicianAvailability", new { quotationId = input.QuotationId });

        Guid? technicianId = quote.TryGetProperty("technicianId", out var techEl) && techEl.ValueKind == JsonValueKind.String
            ? Guid.TryParse(techEl.GetString(), out var parsed) ? parsed : null
            : quote.TryGetProperty("technicianId", out var techGuid) && techGuid.ValueKind == JsonValueKind.String
                ? null
                : quote.TryGetProperty("technicianId", out var t) && t.TryGetGuid(out var g) ? g : null;
        if (quote.TryGetProperty("technicianId", out var technicianProperty))
        {
            technicianId = technicianProperty.ValueKind == JsonValueKind.String
                ? Guid.Parse(technicianProperty.GetString()!)
                : technicianProperty.GetGuid();
        }

        if (technicianId is Guid technician)
        {
            var approvalCheck = await Call("CheckCategoryApproval", new { technicianId = technician, requestId = input.RequestId });
            if (Bool(approvalCheck, "approved") == false)
            {
                errors.Add("Technician is no longer verified for this category.");
            }
        }
        else
        {
            errors.Add("Quotation is missing a technician.");
        }

        if (Bool(ownership, "owns") == false)
        {
            errors.Add("Wrong customer.");
        }

        if (Bool(expiry, "expired") == true)
        {
            errors.Add("Expired quotation.");
        }

        if (Bool(relationship, "matches") == false)
        {
            errors.Add("Quote belongs to another request.");
        }

        if (Bool(approval, "approved") == false)
        {
            errors.Add("Missing human approval.");
        }

        if (Bool(conflict, "conflict") == true)
        {
            errors.Add("Booking conflict.");
        }

        if (Bool(availability, "available") == false)
        {
            errors.Add("Technician unavailable.");
        }

        if (quote.TryGetProperty("labourAmount", out var labour)
            && quote.TryGetProperty("materialsAmount", out var materials)
            && quote.TryGetProperty("travelAmount", out var travel)
            && quote.TryGetProperty("totalAmount", out var total)
            && labour.TryGetDecimal(out var l)
            && materials.TryGetDecimal(out var m)
            && travel.TryGetDecimal(out var tr)
            && total.TryGetDecimal(out var tot)
            && l + m + tr != tot)
        {
            errors.Add("Price mismatch.");
        }

        _ = request;

        var payload = new
        {
            input.RequestId,
            input.QuotationId,
            input.CustomerId,
            errors,
            instruction = "Do not approve booking if any safety check failed. Never invent a pass."
        };

        var raw = await model.CompleteJsonAsync(new AiCompletionRequest
        {
            AgentName = nameof(BookingValidationAgent),
            Objective = "Validate the selected quotation before booking.",
            StructuredInputJson = JsonSerializer.Serialize(payload, AgentJson.Options),
            AllowedTools = tools.AllowedTools(Role),
            MaxOutputChars = maxOutputChars
        }, cancellationToken);

        var output = AgentOutputValidator.ValidateValidation(AgentSafety.Deserialize<ValidationOutput>(raw, maxOutputChars));
        if (errors.Count > 0)
        {
            output.Valid = false;
            output.BookingAllowed = false;
            output.Errors = output.Errors.Union(errors).Distinct().ToList();
        }

        return (output, used.Distinct().ToList());
    }

    private static bool? Bool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
}
