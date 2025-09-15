// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.McpGateway.Management.Configuration
{
    public class CosmosSettings
    {
        /// <summary>
        /// Cosmos DB account endpoint
        /// </summary>
        public string AccountEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Cosmos DB connection string (alternative to AccountEndpoint)
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; } = "McpGatewayDb";

        /// <summary>
        /// Container name for adapter resources
        /// </summary>
        public string AdapterContainerName { get; set; } = "AdapterContainer";

        /// <summary>
        /// Container name for caching
        /// </summary>
        public string CacheContainerName { get; set; } = "CacheContainer";
    }
}
