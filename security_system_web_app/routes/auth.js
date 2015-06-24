var express = require('express');
var router = express.Router();
var env = require('node-env-file');
env('./.env');
var passport = require('passport');
var GitHubStrategy = require('passport-github2').Strategy;
var FacebookStrategy = require('passport-facebook').Strategy;
var userDictionary = require('../allowed-users.js');
var session = require('express-session');


router.get('/auth/github',
  passport.authenticate('github', { scope: [ 'user:email' ] }));

router.get('/auth/github/callback',
  passport.authenticate('github', { failureRedirect: '/login' }),
  function(req, res) {
    // Successful authentication, redirect home.
  console.log("redirection happening");
    res.redirect('/');
  });

router.get('/auth/facebook',
  passport.authenticate('facebook'));

router.get('/auth/facebook/callback',
  passport.authenticate('facebook', { failureRedirect: '/login' }),
  function(req, res) {
    // Successful authentication, redirect home.
  console.log("redirection happening");
    res.redirect('/');
  });

module.exports = router;