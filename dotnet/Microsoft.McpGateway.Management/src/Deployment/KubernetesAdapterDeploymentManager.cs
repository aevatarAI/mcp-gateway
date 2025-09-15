// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.McpGateway.Management.Contracts;
using Microsoft.McpGateway.Management.Extensions;
using Microsoft.McpGateway.Management.Configuration;

namespace Microsoft.McpGateway.Management.Deployment
{
    public class KubernetesAdapterDeploymentManager : IAdapterDeploymentManager
    {
        private readonly IKubeClientWrapper _kubeClient;
        private readonly ContainerRegistrySettings _containerRegistrySettings;
        private readonly KubernetesSettings _kubernetesSettings;
        private readonly ServiceSettings _serviceSettings;
        private readonly ILogger<KubernetesAdapterDeploymentManager> _logger;

        public KubernetesAdapterDeploymentManager(
            ContainerRegistrySettings containerRegistrySettings, 
            KubernetesSettings kubernetesSettings,
            ServiceSettings serviceSettings,
            IKubeClientWrapper kubeClient, 
            ILogger<KubernetesAdapterDeploymentManager> logger)
        {
            _containerRegistrySettings = containerRegistrySettings ?? throw new ArgumentNullException(nameof(containerRegistrySettings));
            _kubernetesSettings = kubernetesSettings ?? throw new ArgumentNullException(nameof(kubernetesSettings));
            _serviceSettings = serviceSettings ?? throw new ArgumentNullException(nameof(serviceSettings));
            _kubeClient = kubeClient ?? throw new ArgumentNullException(nameof(kubeClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ArgumentException.ThrowIfNullOrEmpty(containerRegistrySettings.Endpoint);
            ArgumentException.ThrowIfNullOrEmpty(kubernetesSettings.Namespace);
        }

        public async Task CreateDeploymentAsync(AdapterData request, CancellationToken cancellationToken)
        {
            var labels = new Dictionary<string, string>
            {
                { $"{_kubernetesSettings.LabelPrefix}/type", "mcp" },
                { $"{_kubernetesSettings.LabelPrefix}/name", request.Name }
            };

            var statefulSet = new V1StatefulSet
            {
                Metadata = new V1ObjectMeta { Name = request.Name },
                Spec = new V1StatefulSetSpec
                {
                    ServiceName = $"{request.Name}-service",
                    Replicas = request.ReplicaCount,
                    Selector = new V1LabelSelector { MatchLabels = labels },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta { Labels = labels },
                        Spec = new V1PodSpec
                        {
                            SecurityContext = new V1PodSecurityContext
                            {
                                RunAsUser = _kubernetesSettings.SecurityContext.RunAsUser,
                                RunAsGroup = _kubernetesSettings.SecurityContext.RunAsGroup
                            },
                            Containers =
                            [
                                new()
                                {
                                    Name = $"{request.Name}-container",
                                    Image = $"{_containerRegistrySettings.Endpoint}/{request.ImageName}:{request.ImageVersion}",
                                    ImagePullPolicy = _containerRegistrySettings.ImagePullPolicy,
                                    Env = [.. request.EnvironmentVariables?.Select(x => new V1EnvVar{ Name = x.Key, Value = x.Value }) ?? []],
                                    Ports =
                                    [
                                        new V1ContainerPort
                                        {
                                            ContainerPort = _serviceSettings.AdapterPort,
                                            Protocol = _serviceSettings.Protocol
                                        }
                                    ],
                                    SecurityContext = new V1SecurityContext
                                    {
                                        AllowPrivilegeEscalation = _kubernetesSettings.SecurityContext.AllowPrivilegeEscalation,
                                        ReadOnlyRootFilesystem = _kubernetesSettings.SecurityContext.ReadOnlyRootFilesystem,
                                        Capabilities = new V1Capabilities { Drop = _kubernetesSettings.SecurityContext.DropCapabilities }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new ResourceQuantity(_kubernetesSettings.Resources.Limits.Cpu),
                                            ["memory"] = new ResourceQuantity(_kubernetesSettings.Resources.Limits.Memory),
                                            ["ephemeral-storage"] = new ResourceQuantity(_kubernetesSettings.Resources.Limits.EphemeralStorage)
                                        },
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new ResourceQuantity(_kubernetesSettings.Resources.Requests.Cpu),
                                            ["memory"] = new ResourceQuantity(_kubernetesSettings.Resources.Requests.Memory)
                                        }
                                    }
                                }
                            ]
                        }
                    }
                }
            };

            var service = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = $"{request.Name}-service"
                },
                Spec = new V1ServiceSpec
                {
                    ClusterIP = "None",
                    Selector = labels,
                    Ports =
                    [
                        new()
                        {
                            Port = _serviceSettings.AdapterPort,
                            TargetPort = _serviceSettings.AdapterPort,
                            Protocol = _serviceSettings.Protocol
                        }
                    ]
                }
            };

            _logger.LogInformation("Creating deployment for {name}.", request.Name.Sanitize());
            try
            {
                await _kubeClient.UpsertStatefulSetAsync(statefulSet, _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Submitted Kubernetes deployment for {name}.", request.Name.Sanitize());
            }
            catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogInformation("Kubernetes deployment for {name} already exists. Skip deployment.", request.Name.Sanitize());
            }

            try
            {
                await _kubeClient.UpsertServiceAsync(service, _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Submitted Kubernetes service for {name}.", request.Name.Sanitize());
            }
            catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogInformation("Kubernetes service for {name} already exists. Skip service creation.", request.Name.Sanitize());
            }
        }

        public async Task UpdateDeploymentAsync(AdapterData request, CancellationToken cancellationToken)
        {
            var statefulSet = await _kubeClient.ReadStatefulSetAsync(request.Name, _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
            var patch = new
            {
                spec = new
                {
                    replicas = request.ReplicaCount,
                    template = new
                    {
                        spec = new
                        {
                            containers = new[]
                            {
                                new
                                {
                                    name = $"{request.Name}-container",
                                    image = $"{_containerRegistrySettings.Endpoint}/{request.ImageName}:{request.ImageVersion}",
                                    env = request.EnvironmentVariables.Select(x => new V1EnvVar{ Name = x.Key, Value = x.Value }).ToArray(),
                                }
                            }
                        }
                    }
                }
            };

            var patchContent = new V1Patch(JsonSerializer.Serialize(patch), V1Patch.PatchType.StrategicMergePatch);
            _logger.LogInformation("Updating deployment for {name}.", request.Name.Sanitize());
            await _kubeClient.PatchStatefulSetAsync(patchContent, request.Name, _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Submitted updating deployment for {name}.", request.Name.Sanitize());
        }

        public async Task DeleteDeploymentAsync(string name, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting deployment for {name}.", name.Sanitize());
                await _kubeClient.DeleteStatefulSetAsync(name, _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Submitted deleting deployment for {name}.", name.Sanitize());
                await _kubeClient.DeleteServiceAsync($"{name}-service", _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Submitted deleting service for {name}.", name.Sanitize());
            }
            catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Deployment for {name} does not exist in the cluster.", name.Sanitize());
            }
        }

        public async Task<AdapterStatus> GetDeploymentStatusAsync(string name, CancellationToken cancellationToken)
        {
            var statefulSet = await _kubeClient.ReadStatefulSetAsync(name, _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
            var status = new AdapterStatus
            {
                ReadyReplicas = statefulSet.Status.ReadyReplicas,
                UpdatedReplicas = statefulSet.Status.UpdatedReplicas,
                AvailableReplicas = statefulSet.Status.AvailableReplicas,
                Image = statefulSet.Spec.Template.Spec.Containers.FirstOrDefault()?.Image ?? "Unknown"
            };

            if ((status.ReadyReplicas ?? 0) == (statefulSet.Spec.Replicas ?? 0))
                status.ReplicaStatus = "Healthy";
            else
                status.ReplicaStatus = $"Degraded: {status.ReadyReplicas ?? 0}/{statefulSet.Spec.Replicas ?? 0} ready";

            return status;
        }

        public async Task<string> GetDeploymentLogsAsync(string name, int ordinal = 0, CancellationToken cancellationToken = default)
        {
            var podName = $"{name}-{ordinal}";
            using var logStream = await _kubeClient.GetContainerLogStream(podName, 1000, _kubernetesSettings.Namespace, cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(logStream);
            var logText = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return logText;
        }
    }
}
