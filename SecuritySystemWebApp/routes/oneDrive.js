var express = require('express');
var router = express.Router();
var env = require('node-env-file');
var request = require('request')
env('./.env')
var urlApi = require('url')
var bigInt = require('big-integer');


router.get('/',
function(req, res, next) {
  res.render('oneDriveLogin', { title: 'Express' });
});

router.get('/auth', function(req, res) {
  console.log(process.env.ONE_DRIVE_CALLBACK_URI)
  res.redirect('https://login.live.com/oauth20_authorize.srf?client_id=' + process.env.ONE_DRIVE_CLIENT_ID + '&scope=wl.signin wl.offline_access wl.photos wl.skydrive onedrive.readwrite&response_type=token&redirect_uri=' + process.env.ONE_DRIVE_CALLBACK_URI)
})

router.post('/images', function(req, res) {
  var url = req.body.url,
      days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],
      tokenStart = url.search('access_token'),
      tokenEnd = url.search('&token_type'),
      accessToken = url.slice(tokenStart + 13, tokenEnd);
  request('https://api.onedrive.com/v1.0/drive/root:/pictures/imagecontainer/Cam1/' + req.body.date +':/children?access_token=' + accessToken, function(error, response, body) {
    var parsedBody = JSON.parse(body).value
    if(parsedBody) {
      for(var i = 0; i < parsedBody.length; i++){
        // using npm module bigInt, because the number of .NET ticks
        // is a number with too many digits for vanilla JavaScript
        // to perform accurate math on.
        var ticks = parsedBody[i].name.slice(3,21);
            ticksAtUnixEpoch = bigInt("621355968000000000"),
            ticksInt = bigInt(ticks),
            ticksSinceUnixEpoch = ticksInt.minus(ticksAtUnixEpoch),
            milliseconds = ticksSinceUnixEpoch.divide(10000),
            date = new Date(milliseconds.value),
            day = days[date.getDay()],
            localDate = date.toLocaleString();


        parsedBody[i].date = localDate;
        parsedBody[i].day = day;
        parsedBody[i].hour = parsedBody[i].name.slice(0,2);
      }
      res.send({images: parsedBody})
    }else {
      res.send({images: []})
    }

  })
})


router.get('/oneDrive/callback', function(req,res) {
  res.render('index')
})



module.exports = router;
