// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.McpGateway.Management.Configuration
{
    public class KubernetesSettings
    {
        /// <summary>
        /// The Kubernetes namespace where adapter resources will be deployed
        /// </summary>
        public string Namespace { get; set; } = "adapter";

        /// <summary>
        /// Label prefix used for adapter resources
        /// </summary>
        public string LabelPrefix { get; set; } = "adapter";

        /// <summary>
        /// Security context settings for pods
        /// </summary>
        public SecurityContextSettings SecurityContext { get; set; } = new();

        /// <summary>
        /// Resource limits and requests for containers
        /// </summary>
        public ResourceSettings Resources { get; set; } = new();
    }

    public class SecurityContextSettings
    {
        /// <summary>
        /// User ID to run containers as
        /// </summary>
        public long RunAsUser { get; set; } = 1100;

        /// <summary>
        /// Group ID to run containers as
        /// </summary>
        public long RunAsGroup { get; set; } = 1100;

        /// <summary>
        /// Whether to allow privilege escalation
        /// </summary>
        public bool AllowPrivilegeEscalation { get; set; } = false;

        /// <summary>
        /// Whether to use read-only root filesystem
        /// </summary>
        public bool ReadOnlyRootFilesystem { get; set; } = true;

        /// <summary>
        /// Capabilities to drop (default: ALL)
        /// </summary>
        public string[] DropCapabilities { get; set; } = ["ALL"];
    }

    public class ResourceSettings
    {
        /// <summary>
        /// Resource limits
        /// </summary>
        public ResourceLimits Limits { get; set; } = new();

        /// <summary>
        /// Resource requests
        /// </summary>
        public ResourceRequests Requests { get; set; } = new();
    }

    public class ResourceLimits
    {
        /// <summary>
        /// CPU limit (e.g., "1", "500m")
        /// </summary>
        public string Cpu { get; set; } = "1";

        /// <summary>
        /// Memory limit (e.g., "512Mi", "1Gi")
        /// </summary>
        public string Memory { get; set; } = "512Mi";

        /// <summary>
        /// Ephemeral storage limit (e.g., "2Gi")
        /// </summary>
        public string EphemeralStorage { get; set; } = "2Gi";
    }

    public class ResourceRequests
    {
        /// <summary>
        /// CPU request (e.g., "250m", "0.5")
        /// </summary>
        public string Cpu { get; set; } = "250m";

        /// <summary>
        /// Memory request (e.g., "256Mi", "512Mi")
        /// </summary>
        public string Memory { get; set; } = "256Mi";
    }
}
