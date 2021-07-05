#!/usr/bin/env bash

ownerId=$(az ad signed-in-user show --query "objectId" --output tsv | tr -d '\r')

bootstrapAppId=$(az ad app create --display-name "speedway-bootstrap" --native-app --reply-urls "http://localhost/" --required-resource-accesses @api-permissions.json --output tsv --query "appId" | tr -d '\r') 

echo "[CLI]:: Assigning owner"
_=$(az ad app owner add --id "$bootstrapAppId" --owner-object-id "$ownerId")
echo "[CLI]:: Creating service principal"
_=$(az ad sp create --id "$bootstrapAppId")
echo "[CLI]:: Bootstrap App Id: ${bootstrapAppId}"

echo "Granting Speedway bootstrap admin consent"
az ad app permission admin-consent --id "$bootstrapAppId"
