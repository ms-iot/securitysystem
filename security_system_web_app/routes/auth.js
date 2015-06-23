var express = require('express');
var router = express.Router();
var env = require('node-env-file');
env('./.env');
var passport = require('passport');
var GitHubStrategy = require('passport-github2').Strategy;
var FacebookStrategy = require('passport-facebook').Strategy;
var userDictionary = require('../allowed-users.js');
var session = require('express-session');


// router.use(session({
//   key: 'express.sid',
//   secret: ';alsk08usahjl123n4123',
//   resave: false,
//   saveUninitialized: false}));
// router.use(passport.initialize());
// router.use(passport.session());

passport.use(new GitHubStrategy({
    clientID: process.env.GITHUB_CLIENT_ID,
    clientSecret: process.env.GITHUB_CLIENT_SECRET,
    callbackURL: "http://localhost:3000/auth/github/callback"
  },
  function(accessToken, refreshToken, profile, done) {
    // console.log(profile.id);
  console.log(profile);
  if(userDictionary[profile.username])
  {
    console.log("found user");
    return done(null, profile);
  }else{
    // redirect to error page
    return done(null, null);
  }
  }
));

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

router.get('/github',
  passport.authenticate('github', { scope: [ 'user:email' ] }));

router.get('/github/callback',
  passport.authenticate('github', { failureRedirect: '/login' }),
  function(req, res) {
    // Successful authentication, redirect home.
  console.log("redirection happening");
    res.redirect('/');
  });


function ensureAuthenticated(req, res, next) {
  if (req.isAuthenticated()) {
    console.log('request authenticated');
    return next();
  }
  console.log("not authenticated, going to login page");
  res.redirect('/login');
}

module.exports = router;