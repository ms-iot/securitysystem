var express = require('express');
var session = require('express-session');
var path = require('path');
var favicon = require('serve-favicon');
var logger = require('morgan');
var env = require('node-env-file');
var bodyParser = require('body-parser');
var passport = require('passport');
var authentication = require('./authentication.js')


//.env file
env('./.env')

var fs = require('fs');
var azure = require('azure-storage');
var bigInt = require('big-integer');
var blobService = azure.createBlobService();
var auth = "./routes/auth";
var index = "./routes/index";

var app = express();

// view engine setup
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'ejs');


app.use(favicon(__dirname + '/public/images/favicon.ico'));
app.use(logger('dev'));
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: false }));
app.use(express.static(path.join(__dirname, 'public')));
app.use(session({
  key: 'express.sid',
  secret: ';alsk08usahjl123n4123',
  resave: false,
  saveUninitialized: false}));
app.use(passport.initialize());
app.use(passport.session());
app.use('/auth', auth);
app.use('/', index);


passport.serializeUser(function(user, done) {
  console.log("serializing");
  console.log(user);
  done(null, user);
});

passport.deserializeUser(function(user, done) {
  console.log("deserializing");
  console.log(user);
  done(null, user);
});

app.get('/auth/github',
  passport.authenticate('github', { scope: [ 'user:email' ] }));

app.get('/auth/github/callback',
  passport.authenticate('github', { failureRedirect: '/login' }),
  function(req, res) {
    // Successful authentication, redirect home.
  console.log("redirection happening");
    res.redirect('/');
  });

app.get('/auth/facebook',
  passport.authenticate('facebook'));

app.get('/auth/facebook/callback',
  passport.authenticate('facebook', { failureRedirect: '/login' }),
  function(req, res) {
    // Successful authentication, redirect home.
  console.log("redirection happening");
    res.redirect('/');
  });

app.get('/',
  ensureAuthenticated,
  function(req, res) {
    console.log("opening home");
    res.render('index');
});

function ensureAuthenticated(req, res, next) {
  if (req.isAuthenticated()) {
    console.log('request authenticated');
    return next();
  }
  console.log("not authenticated, going to login page");
  res.redirect('/login');
}

app.get('/images',
  ensureAuthenticated,
  function(req,res){
    var images = [];
        blobService.listBlobsSegmented('imagecontainer', null, function(error, result, response){
          if(!error){
            images = result;
            console.log(images.entries)
             for(var i = 0; i < images.entries.length; i++){
                // using npm module bigInt, because the number of .NET ticks
                // is a number with too many digits for vanilla JavaScript
                // to perform accurate math on.
                var ticks = images.entries[i].name.slice(0,18);
                var ticksAtUnixEpoch = bigInt("621355968000000000")
                var ticksInt = bigInt(ticks);
                var ticksSinceUnixEpoch = ticksInt.minus(ticksAtUnixEpoch);
                var milliseconds = ticksSinceUnixEpoch.divide(10000)
                //Converting millisecond to dateTime client side so it will
                //display in the user's local timezone.
                images.entries[i].milliseconds = milliseconds.value
              }
            res.send(images);
          } else {
            res.send(error);
          }
        })
})

app.get('/login', function(req, res){
  res.render('login');
});

// app.use('/', routes);

// catch 404 and forward to error handler
app.use(function(req, res, next) {
  var err = new Error('Not Found');
  err.status = 404;
  next(err);
});

// error handlers

// development error handler
// will print stacktrace
if (app.get('env') === 'development') {
  app.use(function(err, req, res, next) {
    res.status(err.status || 500);
    res.render('error', {
      message: err.message,
      error: err
    });
  });
}

// production error handler
// no stacktraces leaked to user
app.use(function(err, req, res, next) {
  res.status(err.status || 500);
  res.render('error', {
    message: err.message,
    error: {}
  });
});


module.exports = app;
