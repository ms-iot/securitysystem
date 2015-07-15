var express = require('express');
var router = express.Router();
var env = require('node-env-file');
var request = require('request')
env('./.env')
var urlApi = require('url')
var bigInt = require('big-integer');

/* GET home page. */
router.get('/',
function(req, res, next) {
  res.render('oneDriveLogin', { title: 'Express' });
});

router.get('/auth', function(req, res) {
  res.redirect('https://login.live.com/oauth20_authorize.srf?client_id=' + process.env.ONE_DRIVE_CLIENT_ID + '&scope=wl.signin wl.offline_access wl.photos wl.skydrive onedrive.readwrite&response_type=token&redirect_uri=' + process.env.ONE_DRIVE_CALLBACK_URI)
})

router.post('/images', function(req, res) {
  var url = req.body.url,
      tokenStart = url.search('access_token'),
      tokenEnd = url.search('&token_type'),
      accessToken = url.slice(tokenStart + 13, tokenEnd);
  request('https://api.onedrive.com/v1.0/drive/special/photos:/securitysystem:/children?access_token=' + accessToken, function(error, response, body) {
    var parsedBody = JSON.parse(body).value
    if(parsedBody) {
      for(var i = 0; i < parsedBody.length; i++){
        // using npm module bigInt, because the number of .NET ticks
        // is a number with too many digits for vanilla JavaScript
        // to perform accurate math on.
        var ticks = parsedBody[i].name.slice(0,18),
            ticksAtUnixEpoch = bigInt("621355968000000000"),
            ticksInt = bigInt(ticks),
            ticksSinceUnixEpoch = ticksInt.minus(ticksAtUnixEpoch),
            milliseconds = ticksSinceUnixEpoch.divide(10000);
        //Converting millisecond to dateTime client side so it will
        //display in the user's local timezone.
        parsedBody[i].milliseconds = milliseconds.value
      }
    }
    res.send({images: parsedBody, storageService: "return image['@content.downloadUrl']"})
  })
})


router.get('/oneDrive/callback', function(req,res) {
  res.render('index')
})



module.exports = router;
