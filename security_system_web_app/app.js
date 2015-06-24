var express = require('express');
var session = require('express-session');
var path = require('path');
var favicon = require('serve-favicon');
var logger = require('morgan');
var env = require('node-env-file');
var bodyParser = require('body-parser');
var passport = require('passport');
var authenticationStrategies = require('./authenticationStrategies.js')


//.env file
env('./.env')

var auth = require("./routes/auth");
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
app.use(passport.initialize());
app.use(passport.session());
app.use('/', index);
app.use('/auth', auth);


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
