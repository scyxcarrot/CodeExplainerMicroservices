helm repo update
helm repo add qdrant https://qdrant.github.io/qdrant-helm  
helm install qdrant qdrant/qdrant --set persistence.enabled=true --set persistence.size=1Gi

helm repo add otwld https://helm.otwld.com/
helm repo update
helm upgrade --install ollama otwld/ollama --namespace ollama --create-namespace -f OllamaDeployment.yaml

kubectl create secret generic mssql --from-literal=SA_PASSWORD="pa55w0rd!"
kubectl create secret generic github-model-token --from-literal=GitHubModelToken="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"
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.13.3/deploy/static/provider/cloud/deploy.yaml
kubectl apply -f .\LocalPersistentVolume.yaml
kubectl apply -f .\LocalPersistentVolumeClaim.yaml
kubectl apply -f .\MSSQLDeployment.yaml
kubectl apply -f .\RabbitMQDeployment.yaml
kubectl apply -f .\IngressService.yaml
kubectl apply -f .\UserServiceDeployment.yaml
kubectl apply -f .\ChatServiceDeployment.yaml
kubectl apply -f .\IDSCodeExplainerDeployment.yaml
