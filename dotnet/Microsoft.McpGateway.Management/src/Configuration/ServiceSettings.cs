// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.McpGateway.Management.Configuration
{
    public class ServiceSettings
    {
        /// <summary>
        /// Port for the main service to listen on
        /// </summary>
        public int Port { get; set; } = 8000;

        /// <summary>
        /// Port for adapter containers
        /// </summary>
        public int AdapterPort { get; set; } = 8000;

        /// <summary>
        /// Protocol for service communication
        /// </summary>
        public string Protocol { get; set; } = "TCP";
    }
}
