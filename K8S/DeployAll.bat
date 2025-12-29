REM --- Helm repos ---
helm repo add qdrant https://qdrant.github.io/qdrant-helm
helm repo update
helm install qdrant qdrant/qdrant -f QDrantDeployment.yaml

helm repo add otwld https://helm.otwld.com/
helm repo update
helm upgrade --install ollama otwld/ollama --namespace ollama --create-namespace -f OllamaDeployment.yaml

REM --- Kubernetes secrets ---
kubectl create secret generic mssql --from-literal=SA_PASSWORD="pa55w0rd!"
kubectl create secret generic github-model-token --from-literal=GitHubModelToken="github_pat_1XXX"

REM --- Apply manifests ---
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.13.3/deploy/static/provider/cloud/deploy.yaml
kubectl apply -f .\LocalPersistentVolume.yaml
kubectl apply -f .\LocalPersistentVolumeClaim.yaml
kubectl apply -f .\MSSQLDeployment.yaml
kubectl apply -f .\RabbitMQDeployment.yaml
kubectl apply -f .\IngressService.yaml
kubectl apply -f .\UserServiceDeployment.yaml
kubectl apply -f .\ChatServiceDeployment.yaml
kubectl apply -f .\IDSCodeExplainerDeployment.yaml
