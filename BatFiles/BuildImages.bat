docker build -t justinwcy/code_explainer_user_service -f UserService/Dockerfile .
docker build -t justinwcy/code_explainer_ids_code_explainer -f IDSCodeExplainer/Dockerfile .
docker build -t justinwcy/code_explainer_chat_service -f ChatService/Dockerfile .