var securitySystem = angular.module('securitySystem', ['ngRoute', 'ngMaterial', 'infinite-scroll']);

securitySystem.config(['$routeProvider','$locationProvider',function($routeProvider, $locationProvider){


  $routeProvider
  .when('/',{
    templateUrl:'javascripts/angular/views/home.html'
  })

  $locationProvider.hashPrefix('!')
}]);