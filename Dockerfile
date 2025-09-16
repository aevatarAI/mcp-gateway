# Use the official Microsoft .NET 8 ASP.NET runtime image as the base.
# This image is optimized for running ASP.NET Core applications (no SDK).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Create a non-root user and group with fixed UID/GID (1100) for security.
# Running as non-root prevents privilege escalation inside the container.
RUN addgroup --gid 1100 mcpgroup \
    && adduser --disabled-password --gecos '' --uid 1100 --gid 1100 mcpuser

# Set the working directory inside the container to /app.
# All subsequent commands will be run relative to this folder.
WORKDIR /app

# Define a build argument for the service name.
# This allows reusing the Dockerfile for different services/projects.
ARG servicename

# Copy the published application output (from GitHub Actions artifact) 
# into the container's /app directory.
COPY out/${servicename}/ ./

# Fix ownership of /app so the non-root user has permissions.
RUN chown -R mcpuser:mcpgroup /app

# Switch to the non-root user for running the application.
USER mcpuser

# Expose the application port (default here: 8000).
# This makes the port accessible when the container runs.
EXPOSE 8000

# Set the default entrypoint to run the .NET application.
# The DLL name is based on the service name passed via ARG.
ENTRYPOINT ["dotnet", "${servicename}.dll"]