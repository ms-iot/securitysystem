var securitySystem = angular.module('securitySystem', ['ngRoute', 'ui.bootstrap']);

securitySystem.config(['$routeProvider','$locationProvider',function($routeProvider, $locationProvider){

  // $locationProvider.html5Mode(true);

  $routeProvider
  .when('/',{
    templateUrl:'javascripts/angular/views/home.html'
  })
  .when('/mobileNav',{
    templateUrl:'javascripts/angular/views/mobileNav.html'
  })

  $locationProvider.hashPrefix('!')
}]);