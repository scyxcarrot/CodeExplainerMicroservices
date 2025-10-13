kubectl create secret generic mssql --from-literal=SA_PASSWORD="pa55w0rd!"
kubectl create secret generic github-model-token --from-literal=GitHubModelToken="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"
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.13.3/deploy/static/provider/cloud/deploy.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\LocalPersistentVolume.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\LocalPersistentVolumeClaim.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\MSSQLDeployment.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\RabbitMQDeployment.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\IngressService.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\UserServiceDeployment.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\ChatServiceDeployment.yaml
kubectl apply -f C:\Users\jwong\Desktop\tutorial\CodeExplainer\K8S\IDSCodeExplainerDeployment.yaml
