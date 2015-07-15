var express = require('express');
var router = express.Router();
var env = require('node-env-file');
env('./.env')
var fs = require('fs');
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
        expiryDate = new Date(startDate);
    expiryDate.setMinutes(startDate.getMinutes() + 10);
    startDate.setMinutes(startDate.getMinutes() - 10);
    var sharedAccessPolicy = {
      AccessPolicy: {
        Permissions: azure.BlobUtilities.SharedAccessPermissions.READ,
        Start: startDate,
        Expiry: expiryDate
      },
      };
        blobService.listBlobsSegmented('imagecontainer', null, function(error, result, response){
          if(!error){
            images = result;
             for(var i = 0; i < images.entries.length; i++){
                // using npm module bigInt, because the number of .NET ticks
                // is a number with too many digits for vanilla JavaScript
                // to perform accurate math on.
                var ticks = images.entries[i].name.slice(0,18),
                    ticksAtUnixEpoch = bigInt("621355968000000000"),
                    ticksInt = bigInt(ticks),
                    ticksSinceUnixEpoch = ticksInt.minus(ticksAtUnixEpoch),
                    milliseconds = ticksSinceUnixEpoch.divide(10000);
                //Converting millisecond to dateTime client side so it will
                //display in the user's local timezone.
                images.entries[i].milliseconds = milliseconds.value
                var token = blobService.generateSharedAccessSignature('imagecontainer', images.entries[i].name, sharedAccessPolicy);
                images.entries[i].imageUrl = blobService.getUrl('imagecontainer', images.entries[i].name, token);
              }
            console.log(images)
            res.send({images: images.entries, storageService: "return image['imageUrl']"});
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
