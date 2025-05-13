
az login

az acr login --name $acrName


docker tag $image $acrName.azurecr.io/$image$version

docker push $acrName.azurecr.io/$image$version

az containerapp env create --name $containerName  --resource-group $resourceGroupName --locaion $location 

az containerapp create --name $containerName --resource-group $resourceGroupName --environment $containerName --image $acrName.azurecr.io/$image$version --cpu 0.5 --memory 1.0Gi --ingress external --target-port $port --min-replicas 1 --max-replicas 3 --revision-suffix $version --registry-server $acrName.azurecr.io az containerapp create --name rent-a-car --resource-group Lab07 --environment rent-a-car --image acrerodrigues.azurecr.io/rent-a-car:v1 --ingress external --target-port 80  --registry-server acrerodrigues.azurecr.io


az containerapp create --name rent-a-car --resource-group Lab07 --environment rent-a-car --image acrerodrigues.azurecr.io/rent-a-car:v1 --ingress 'external' --target-port 3001  --registry-server acrerodrigues.azurecr.io
