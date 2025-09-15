#!/bin/bash

# Deploy Official MCP Servers Script
# This script deploys official MCP server Docker images using the MCP Gateway API

set -e

# Configuration
GATEWAY_URL="http://localhost:8000"
REGISTRY="docker.io"  # Docker Hub registry

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to deploy an MCP server
deploy_mcp_server() {
    local name=$1
    local image_name=$2
    local image_version=${3:-latest}
    local description=$4
    
    print_status "Deploying $name MCP server..."
    
    curl -X POST "$GATEWAY_URL/adapters" \
        -H "Content-Type: application/json" \
        -d "{
            \"name\": \"$name\",
            \"imageName\": \"$image_name\",
            \"imageVersion\": \"$image_version\",
            \"description\": \"$description\"
        }" \
        --fail --silent --show-error || {
        print_error "Failed to deploy $name"
        return 1
    }
    
    print_status "$name deployed successfully"
}

# Function to check if MCP Gateway is running
check_gateway() {
    print_status "Checking MCP Gateway availability..."
    
    if ! curl -f -s "$GATEWAY_URL/adapters" > /dev/null; then
        print_error "MCP Gateway is not accessible at $GATEWAY_URL"
        print_error "Please ensure the gateway is running and accessible"
        exit 1
    fi
    
    print_status "MCP Gateway is accessible"
}

# Main deployment function
main() {
    echo "==================================="
    echo "Official MCP Servers Deployment"
    echo "==================================="
    
    check_gateway
    
    # Deploy official MCP servers
    print_status "Starting deployment of official MCP servers..."
    
    # Time Server
    deploy_mcp_server "time" "mcp/time" "latest" "Official MCP Time Server - provides time-related tools and utilities"
    
    # Fetch Server
    deploy_mcp_server "fetch" "mcp/fetch" "latest" "Official MCP Fetch Server - provides HTTP request and web scraping capabilities"
    
    # Filesystem Server
    deploy_mcp_server "filesystem" "mcp/filesystem" "latest" "Official MCP Filesystem Server - provides file system operations"
    
    # Git Server
    deploy_mcp_server "git" "mcp/git" "latest" "Official MCP Git Server - provides git repository operations"
    
    # Memory Server
    deploy_mcp_server "memory" "mcp/memory" "latest" "Official MCP Memory Server - knowledge graph memory server for persistent information storage"
    
    # Sequential Thinking Server
    deploy_mcp_server "sequentialthinking" "mcp/sequentialthinking" "latest" "Official MCP Sequential Thinking Server - provides structured thinking capabilities"
    
    echo ""
    print_status "All MCP servers deployed successfully!"
    
    echo ""
    echo "==================================="
    echo "Access Information"
    echo "==================================="
    echo "You can now access these MCP servers through the gateway:"
    echo ""
    echo "• Time Server:             $GATEWAY_URL/adapters/time/mcp"
    echo "• Fetch Server:            $GATEWAY_URL/adapters/fetch/mcp"
    echo "• Filesystem Server:       $GATEWAY_URL/adapters/filesystem/mcp"
    echo "• Git Server:              $GATEWAY_URL/adapters/git/mcp"
    echo "• Memory Server:           $GATEWAY_URL/adapters/memory/mcp"
    echo "• Sequential Thinking:     $GATEWAY_URL/adapters/sequentialthinking/mcp"
    echo ""
    echo "Example VS Code mcp.json configuration:"
    echo '{'
    echo '  "servers": {'
    echo '    "time": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/time/mcp\""
    echo '    },'
    echo '    "fetch": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/fetch/mcp\""
    echo '    },'
    echo '    "filesystem": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/filesystem/mcp\""
    echo '    },'
    echo '    "git": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/git/mcp\""
    echo '    },'
    echo '    "memory": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/memory/mcp\""
    echo '    },'
    echo '    "sequentialthinking": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/sequentialthinking/mcp\""
    echo '    }'
    echo '  }'
    echo '}'
}

# Run the main function
main "$@"
