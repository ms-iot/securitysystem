var env = require('node-env-file');
var GitHubStrategy = require('passport-github2').Strategy;
var FacebookStrategy = require('passport-facebook').Strategy;
var passport = require('passport');
var userDictionary = require('./allowed-users.js');
env('./.env')

passport.use(new GitHubStrategy({
    clientID: process.env.GITHUB_CLIENT_ID,
    clientSecret: process.env.GITHUB_CLIENT_SECRET,
    callbackURL: "http://localhost:3000/auth/github/callback"
    // Change URL when we go live!
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

passport.use(new FacebookStrategy({
    clientID: process.env.FACEBOOK_APP_ID,
    clientSecret: process.env.FACEBOOK_APP_SECRET,
    callbackURL: "http://localhost:3000/auth/facebook/callback",
    // Change URL when we go live!
    enableProof: false
  },
  function(accessToken, refreshToken, profile, done) {
    console.log(profile);
  if(userDictionary[profile.emails[0].value])
  {
    console.log("found user");
    return done(null, profile);
  }else{
    // redirect to error page
    return done(null, null);
  }
  }
));