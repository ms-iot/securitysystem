#SecuritySystem

##Setting Up Azure
1. Create a .env file in the root of `security_system_web_app` and add:
```
AZURE_STORAGE_ACCOUNT={YOUR_ACCOUNT_NAME}
AZURE_STORAGE_ACCESS_KEY={YOUR_ACCOUNT_KEY}
```
2. Push this to Azure

##Setting Up Your Camera

##Debugging

If you are not seeing images appear in BLOB storage, it is possible that your RPi2's time is out of sync. If this is the case, you should sync it before proceeding. Otherwise the Azure authentication will fail.