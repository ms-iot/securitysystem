var express = require('express');
var router = express.Router();
var env = require('node-env-file');
env('./.env')
var fs = require('fs');
var timeFormat = require('../timeFormat.js');
var azure = require('azure-storage');
var bigInt = require('big-integer');
var blobService = azure.createBlobService();

router.get('/',
  ensureAuthenticated,
  function(req, res) {
    console.log("opening home");
    res.render('index');
});

router.post('/images',
  ensureAuthenticated,
  function(req,res){
    var images = [],
        startDate = new Date(),
        expiryDate = new Date(startDate),
        searchDate = req.body.date;
    expiryDate.setMinutes(startDate.getMinutes() + 10);
    startDate.setMinutes(startDate.getMinutes() - 10);
    var sharedAccessPolicy = {
      AccessPolicy: {
        Permissions: azure.BlobUtilities.SharedAccessPermissions.READ,
        Start: startDate,
        Expiry: expiryDate
      },
      };
        blobService.listBlobsSegmentedWithPrefix('securitysystem-cameradrop', "Cam1/" + searchDate, null, function(error, result, response){
          if(!error){
            images = result;
             for(var i = 0; i < images.entries.length; i++){
                images.entries[i] = timeFormat.format(images.entries[i], {name: [19,37], hour: [16,18]})
                token = blobService.generateSharedAccessSignature('securitysystem-cameradrop', images.entries[i].name, sharedAccessPolicy);
                images.entries[i].downloadUrl = blobService.getUrl('securitysystem-cameradrop', images.entries[i].name, token);
              }
            res.send({images: images.entries, token: result.continuationToken});
          } else {
            res.send(error);
          }
        })

})


router.get('/login', function(req, res){
  res.render('login');
});

function ensureAuthenticated(req, res, next) {
  if (req.isAuthenticated() || req.session.passportInitialized === false) {
    return next();
  }
  console.log("not authenticated, going to login page");
  res.redirect('/login');
}

module.exports = router;
