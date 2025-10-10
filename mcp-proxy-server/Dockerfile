FROM python:3.12-slim

ENV DEBIAN_FRONTEND=noninteractive

# Install Node.js + dependencies in one layer to reduce size
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates gnupg git tini dumb-init \
 && mkdir -p /etc/apt/keyrings \
 && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | tee /etc/apt/keyrings/nodesource.gpg > /dev/null \
 && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" \
      | tee /etc/apt/sources.list.d/nodesource.list \
 && apt-get update && apt-get install -y nodejs \
 && curl -LsSf https://astral.sh/uv/install.sh | env UV_INSTALL_DIR=/usr/local/bin UV_NO_MODIFY_PATH=1 sh \
 && apt-get clean && rm -rf /var/lib/apt/lists/*

ENV PATH="/root/.local/bin:${PATH}"

RUN python --version && node --version && npm --version && npx --version && uv --version

# Create non-root user and ensure home directory exists
RUN useradd -m -u 1100 -s /bin/bash mcpuser \
 && mkdir -p /home/mcpuser/.npm \
 && chown -R mcpuser:mcpuser /home/mcpuser

ENV HOME=/home/mcpuser
WORKDIR /app

# Install Python dependencies
COPY requirements.txt .
RUN pip install --upgrade pip --root-user-action=ignore \
 && pip install --no-cache-dir -r requirements.txt --root-user-action=ignore

# Copy source code and fix ownership
COPY . .
RUN chown -R mcpuser:mcpuser /app

USER mcpuser
EXPOSE 8080

CMD ["fastmcp", "run", "src/main.py:app", "--transport", "streamable-http", "--port", "8000", "--host", "0.0.0.0"]