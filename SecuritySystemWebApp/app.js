var express = require('express');
var session = require('express-session');
var path = require('path');
var favicon = require('serve-favicon');
var logger = require('morgan');
var env = require('node-env-file');
var bodyParser = require('body-parser');
var passport = require('passport');
var authenticationStrategies = require('./authenticationStrategies.js')

//set to 'true' if you want authentication AND are using Azure Blob storage.
var authenticationOn = false

// must be set to "oneDrive" or "azure"
var storageService = "azure"


//.env file
env('./.env')

var auth = require("./routes/auth");
var oneDrive = require("./routes/oneDrive")
var index = require("./routes/index");

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
app.use(function (req, res, next) {
  req.session.passportInitialized = authenticationOn
  next();
});

if(authenticationOn == true) {
  app.use(passport.initialize());
  app.use(passport.session());

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
  app.use('/auth', auth);
}

if(storageService === "oneDrive") {
  app.use('/', oneDrive)
} else {
  app.use('/', index);
}

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
