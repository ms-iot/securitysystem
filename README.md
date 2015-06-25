#Security System

Home security systems are a growing field of projects for Makers. A self-built system is not only less expensive than a bulky professional installation, but it also allows for total control and customization to suit your needs. This project uses a D-Link IP camera, a Raspberry Pi 2, an AngularJS web app and a Node.js webserver to create a home security camera. When motion is detected, the camera sends pictures at 1 second intervals to the Raspberry Pi, via FTP, throughout the course of the detected motion. The Pi then acts as a gateway between the camera and the Azure back-end by sending the images to Blob storage as they come in. The Pi also deletes images older than a week from Azure to optimize storage space. An AngularJS web app is used to view these images. 

To build this project, you will only need a Raspberry Pi 2 with an SD card and a D-Link DCS-932L camera. 
The step-by-step instruction for building this project can be found on http://microsoft.hackster.io/en-US/windowsiot/security-system.


##Debugging

If you are not seeing images appear in BLOB storage, it is possible that your RPi2's time is out of sync. If this is the case, you should sync it before proceeding. Otherwise the Azure authentication will fail.