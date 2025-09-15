// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.McpGateway.Management.Configuration
{
    public class ContainerRegistrySettings
    {
        /// <summary>
        /// Container registry endpoint (e.g., "localhost:5000", "myregistry.azurecr.io")
        /// </summary>
        public string Endpoint { get; set; } = "localhost:5000";

        /// <summary>
        /// Username for registry authentication (optional)
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Password for registry authentication (optional)
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Image pull policy (Always, IfNotPresent, Never)
        /// </summary>
        public string ImagePullPolicy { get; set; } = "Always";

        /// <summary>
        /// Default image tag if not specified
        /// </summary>
        public string DefaultTag { get; set; } = "latest";
    }
}
