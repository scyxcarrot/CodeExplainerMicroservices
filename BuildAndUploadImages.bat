docker build --no-cache -t justinwcy/code_explainer_user_service -f UserService/Dockerfile .
docker build --no-cache -t justinwcy/code_explainer_ids_code_explainer -f IDSCodeExplainer/Dockerfile .
docker build --no-cache -t justinwcy/code_explainer_chat_service -f ChatService/Dockerfile .

docker push justinwcy/code_explainer_user_service
docker push justinwcy/code_explainer_ids_code_explainer
docker push justinwcy/code_explainer_chat_service