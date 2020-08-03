# How to add roles using Bash shell with Azure CLI

Retrieve the Identity object ID of the Azure function instance that you deployed in Azure.

![Identity of Azure function instance](./img/identify-id.png)

Get the ID of the Azure Storage account(s) to be accessed by Azure functions.

![ID of storage account](./img/storage-id.png)

And then provide "Storage Blob Data Contributor" and "Reader and Data Access" roles.

```bash
az role assignment create --role "Storage Blob Data Contributor" --assignee-object-id "b8ebe886-2588-4241-98e7-653054d552d5" --scope "/subscriptions/28a75405-95db-4d15-9a7f-ab84003a63aa/resourceGroups/xpouyatdemo/providers/Microsoft.Storage/storageAccounts/xpouyatdemostor"

az role assignment create --role "Reader and Data Access" --assignee-object-id "b8ebe886-2588-4241-98e7-653054d552d5" --scope "/subscriptions/28a75405-95db-4d15-9a7f-ab84003a63aa/resourceGroups/xpouyatdemo/providers/Microsoft.Storage/storageAccounts/xpouyatdemostor"
```

This should provide the needed rights for your Azure functions.
