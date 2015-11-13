#Security System


Home security systems are a growing field of projects for Makers. A self-built system is not only less expensive than a bulky professional installation, but it also allows for total control and customization to suit your needs. 

This project support several configurations:
  1. The base configuration uses a USB camera and PIR motion sensor
  2. Alternatively, a D-Link IP camera can be used for both sensing motion and taking pictures
  3. Additionally, photos can be stored in OneDrive
  4. Additionally, photos can be stored in Azure blob storage, and a website can be deployed to Azure ot view the photos

To build this project, clone/download the project then use Command Prompt to navigate to the root folder of the project:  
```cd <your folder path>\securitysystem-master```  

Next, get the submodules for the USB camera and the PIR sensor by running the following commands:  
```git submodule init```  
```git submodule update```  

The step-by-step instruction for building this project can be found on https://www.hackster.io/windowsiot/security-camera.
