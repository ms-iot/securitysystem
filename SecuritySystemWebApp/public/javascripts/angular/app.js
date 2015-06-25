var securitySystem = angular.module('securitySystem', ['ngRoute', 'ui.bootstrap', 'ngSanitize']);

securitySystem.config(['$routeProvider','$locationProvider',function($routeProvider, $locationProvider){


  $routeProvider
  .when('/',{
    templateUrl:'javascripts/angular/views/home.html'
  })
  .when('/mobileNav',{
    templateUrl:'javascripts/angular/views/mobileNav.html'
  })

  $locationProvider.hashPrefix('!')
}]);