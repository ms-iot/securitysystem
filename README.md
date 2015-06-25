#SecuritySystem


##Setting Up Azure Credentials
1. Create a .env file in the root of `security_system_web_app` and add:
```

AZURE_STORAGE_ACCOUNT={YOUR_ACCOUNT_NAME}
AZURE_STORAGE_ACCESS_KEY={YOUR_ACCOUNT_KEY}
```

##Configuring OAuth with PassportJS
1. For each service you'd like to use for authorization, you must register a new app.
  - Facebook: https://developers.facebook.com/apps/
  - GitHub: https://github.com/settings/developers

2. Once you've registered the app, take the credentials assigned to you and add them to your .env file as follows:
```
GITHUB_CLIENT_ID={YOUR_GITHUB_CLIENT_ID}
GITHUB_CLIENT_SECRET={YOUR_GITHUB_CLIENT_SECRET}
FACEBOOK_APP_ID={YOUR_FACEBOOK_APP_ID}
FACEBOOK_APP_SECRET={YOUR_FACEBOOK_APP_SECRET}
```

3. Rather than creating an entire database structure to house the user information, all users you'd like to be able to access this app must be added manually to 'allowed-users.js' in the following format:
```

var users = {
  "{USER_GITHUB_USERNAME}" : "GitHubUsername",
  "{USER_FACEBOOK_EMAIL}" : "FacebookEmail"
};
```
For GitHub auth, you must provide the GitHub username of anyone you'd like to be able to access the site, and for Facebook, you must provide the primary email associated with whichever Facebook user to whom you'd like to grant access.  Because the passport strategies for Facebook and GitHub search only for the key inside of the users object, be sure not to place the actual username and e-mail as the value in the object's key-value pair.

From this point, you can grant access to as many GitHub users as you like, but in order to allow anyone but the Facebook app creator to access the site, you must take your app out of development mode in the app settings on the Facebook Developer's page.


##Setting Up Your Camera
1. Factory reset your camera by pressing the reset button under the power light. Use a paper clip to press and hold the button for 10 seconds.
2. Connect the camera to the same router/Ethernet switch as your PC and Pi, then connect the power supply to the camera.
3. Set up a D-Link account from https://www.mydlink.com/download. Make a note of your camera IP Address.
4. Enter the IP address on your browser and sign in to the camera portal. 
5. Navigate to Setup->FTP and configure the FTP transfer of pictures. Use the following information:
   FTP SERVER
   - Host Name: The IP address of your Raspberry Pi 2
   - Port: 21
   - Username: administrator
   - Password: The password set for you Pi (default: p@ssw0rd)
   - Path: \Users\DefaultAccount\Pictures

   TIME SCHEDULE
   - Check "Motion/Sound Detection"
   - Choose the Image Frequency of your choice
   - Select "Date/Time Suffix" and check Create sub folder by 0.5hrs.

6. Next, change the motion detection setting of the camera.
   MOTION DETECTION SETTINGS
   - Motion Detection: Select "Enable"
   - Time: Select "Always"
   - Sensitivity: Enter a value of your choice.
   - Detection Areas: Select all areas unless there are some area where motion need not be detected.


##Debugging

If you are not seeing images appear in BLOB storage, it is possible that your RPi2's time is out of sync. If this is the case, you should sync it before proceeding. Otherwise the Azure authentication will fail.