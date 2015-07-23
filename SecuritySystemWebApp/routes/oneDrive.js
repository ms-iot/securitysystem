var express = require('express');
var router = express.Router();
var env = require('node-env-file');
var request = require('request');
var timeFormat = require('../timeFormat.js');
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
      tokenStart = url.search('access_token'),
      tokenEnd = url.search('&token_type'),
      accessToken = url.slice(tokenStart + 13, tokenEnd);
  request('https://api.onedrive.com/v1.0/drive/root:/pictures/imagecontainer/Cam1/' + req.body.date +':/children?access_token=' + accessToken, function(error, response, body) {
    var parsedBody = JSON.parse(body).value
    if(parsedBody) {
      for(var i = 0; i < parsedBody.length; i++){
        parsedBody[i] = timeFormat.format(parsedBody[i], {name: [3,21], hour: [0,2]})
        parsedBody[i].downloadUrl = parsedBody[i]['@content.downloadUrl'];
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
