podman build -t justinwcy/code_explainer_user_service -f UserService/Dockerfile .
podman build -t justinwcy/code_explainer_ids_code_explainer -f IDSCodeExplainer/Dockerfile .
podman build -t justinwcy/code_explainer_chat_service -f ChatService/Dockerfile .