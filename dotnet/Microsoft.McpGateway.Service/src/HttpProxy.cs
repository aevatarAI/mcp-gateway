// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.McpGateway.Service;

public static class HttpProxy
{
    public static async Task<HttpRequestMessage> CreateProxiedHttpRequestAsync(HttpContext context,
        Func<Uri, Uri>? targetOverride = null, CancellationToken cancellationToken = default)
    {
        HttpContent? content = null;

        if (context.Request.ContentLength > 0)
        {
            // Read the request body into memory to avoid stream consumption issues
            var bodyBytes = await ReadRequestBodyAsync(context.Request.Body, cancellationToken);
            content = new ByteArrayContent(bodyBytes);

            // Copy content headers
            if (context.Request.ContentType != null)
                content.Headers.TryAddWithoutValidation(HeaderNames.ContentType, context.Request.ContentType);
            if (context.Request.ContentLength.HasValue)
                content.Headers.TryAddWithoutValidation(HeaderNames.ContentLength,
                    context.Request.ContentLength.Value.ToString());
        }

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = targetOverride == null
                ? new Uri(context.Request.GetEncodedUrl())
                : targetOverride(new Uri(context.Request.GetEncodedUrl())),
            Content = content
        };

        foreach (var header in context.Request.Headers)
        {
            // Skip the inbound Authorization header and content headers
            if (string.Equals(header.Key, HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(header.Key, HeaderNames.ContentType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(header.Key, HeaderNames.ContentLength, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]))
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
        }

        requestMessage.Headers.TryAddWithoutValidation("Forwarded",
            $"for={context.Connection.RemoteIpAddress};proto={context.Request.Scheme};host={context.Request.Host.Value}");
        return requestMessage;
    }

    private static async Task<byte[]> ReadRequestBodyAsync(Stream requestBody, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await requestBody.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    public static Task CopyProxiedHttpResponseAsync(HttpContext context, HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();
        foreach (var header in response.Content.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        context.Response.Headers.Remove(HeaderNames.TransferEncoding);

        return response.Content.CopyToAsync(context.Response.Body, cancellationToken);
    }
}