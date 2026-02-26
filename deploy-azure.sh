#!/bin/bash
set -euo pipefail

# ============================================================
# OneIncTask - Azure Container Apps Deployment
# ============================================================
#
# Prerequisites:
#   1. Azure CLI installed: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
#   2. Logged in: az login
#   3. Run from the project root directory (where docker-compose.yml is)
#
# Usage:
#   chmod +x deploy-azure.sh
#   ./deploy-azure.sh
#
# To tear down everything:
#   az group delete --name oneinctask-rg --yes --no-wait
# ============================================================

# ---------- Configuration (edit these) ----------
RESOURCE_GROUP="oneinctask-rg"
LOCATION="eastus"
ACR_NAME="oneinctaskacr$(openssl rand -hex 3)"  # must be globally unique
ENVIRONMENT="oneinctask-env"
DB_SERVER="oneinctask-db-$(openssl rand -hex 3)"
DB_NAME="oneincdb"
DB_ADMIN="pgadmin"
DB_PASSWORD="P@ss$(openssl rand -hex 12)"
JWT_SECRET="$(openssl rand -base64 48)"

echo "============================================"
echo "  OneIncTask Azure Deployment"
echo "============================================"
echo ""
echo "Resource Group:  $RESOURCE_GROUP"
echo "Location:        $LOCATION"
echo "ACR Name:        $ACR_NAME"
echo "DB Server:       $DB_SERVER"
echo ""

# ---------- Step 1: Resource Group ----------
echo "[1/8] Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ---------- Step 2: Azure Container Registry ----------
echo "[2/8] Creating Container Registry..."
az acr create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$ACR_NAME" \
  --sku Basic \
  --admin-enabled true \
  --output none

ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" -o tsv)

# ---------- Step 3: Build & push images locally ----------
echo "[3/8] Building and pushing images to ACR (this may take a few minutes)..."

echo "  Logging into ACR..."
docker login "$ACR_LOGIN_SERVER" -u "$ACR_NAME" -p "$ACR_PASSWORD"

echo "  Building API image..."
docker build -t "${ACR_LOGIN_SERVER}/oneinc-api:latest" -f src/backend/Dockerfile src/backend/
docker push "${ACR_LOGIN_SERVER}/oneinc-api:latest"

echo "  Building Frontend image..."
docker build -t "${ACR_LOGIN_SERVER}/oneinc-frontend:latest" -f src/frontend/Dockerfile src/frontend/
docker push "${ACR_LOGIN_SERVER}/oneinc-frontend:latest"

echo "  Building Nginx image..."
docker build -t "${ACR_LOGIN_SERVER}/oneinc-nginx:latest" -f nginx/Dockerfile nginx/
docker push "${ACR_LOGIN_SERVER}/oneinc-nginx:latest"

# ---------- Step 4: PostgreSQL Flexible Server ----------
echo "[4/8] Creating PostgreSQL Flexible Server..."
az postgres flexible-server create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DB_SERVER" \
  --location "$LOCATION" \
  --admin-user "$DB_ADMIN" \
  --admin-password "$DB_PASSWORD" \
  --database-name "$DB_NAME" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0 \
  --yes \
  --output none

# Allow Azure services to connect
az postgres flexible-server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DB_SERVER" \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  --output none

DB_CONNECTION="Host=${DB_SERVER}.postgres.database.azure.com;Port=5432;Database=${DB_NAME};Username=${DB_ADMIN};Password=${DB_PASSWORD};SSL Mode=Require;Trust Server Certificate=true"

# ---------- Step 5: Container Apps Environment ----------
echo "[5/8] Creating Container Apps Environment..."
az containerapp env create \
  --name "$ENVIRONMENT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ---------- Step 6: Deploy API (internal ingress) ----------
echo "[6/8] Deploying API container app..."
az containerapp create \
  --name oneinc-api \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$ENVIRONMENT" \
  --image "${ACR_LOGIN_SERVER}/oneinc-api:latest" \
  --registry-server "$ACR_LOGIN_SERVER" \
  --registry-username "$ACR_NAME" \
  --registry-password "$ACR_PASSWORD" \
  --target-port 8080 \
  --ingress internal \
  --transport http \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    "ConnectionStrings__DefaultConnection=${DB_CONNECTION}" \
    "Jwt__Secret=${JWT_SECRET}" \
    Jwt__Issuer=OneIncTask \
    Jwt__Audience=OneIncTask \
    Jwt__ExpirationInMinutes=480 \
  --output none

# ---------- Step 7: Deploy Frontend (internal ingress) ----------
echo "[7/8] Deploying Frontend container app..."
az containerapp create \
  --name oneinc-frontend \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$ENVIRONMENT" \
  --image "${ACR_LOGIN_SERVER}/oneinc-frontend:latest" \
  --registry-server "$ACR_LOGIN_SERVER" \
  --registry-username "$ACR_NAME" \
  --registry-password "$ACR_PASSWORD" \
  --target-port 80 \
  --ingress internal \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.25 \
  --memory 0.5Gi \
  --output none

# ---------- Step 8: Deploy Nginx (external ingress) ----------
echo "[8/8] Deploying Nginx container app..."
az containerapp create \
  --name oneinc-nginx \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$ENVIRONMENT" \
  --image "${ACR_LOGIN_SERVER}/oneinc-nginx:latest" \
  --registry-server "$ACR_LOGIN_SERVER" \
  --registry-username "$ACR_NAME" \
  --registry-password "$ACR_PASSWORD" \
  --target-port 80 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.25 \
  --memory 0.5Gi \
  --env-vars \
    API_UPSTREAM=oneinc-api:80 \
    FRONTEND_UPSTREAM=oneinc-frontend:80 \
  --output none

# ---------- Done ----------
FQDN=$(az containerapp show \
  --name oneinc-nginx \
  --resource-group "$RESOURCE_GROUP" \
  --query "properties.configuration.ingress.fqdn" -o tsv)

echo ""
echo "============================================"
echo "  Deployment Complete!"
echo "============================================"
echo ""
echo "Application URL:  https://${FQDN}"
echo "Credentials:      admin / password123"
echo ""
echo "--- Resources Created ---"
echo "Resource Group:   $RESOURCE_GROUP"
echo "ACR:              $ACR_NAME"
echo "PostgreSQL:       ${DB_SERVER}.postgres.database.azure.com"
echo "DB Password:      $DB_PASSWORD"
echo "JWT Secret:       $JWT_SECRET"
echo ""
echo "Save these credentials! To tear down:"
echo "  az group delete --name $RESOURCE_GROUP --yes --no-wait"
echo ""
