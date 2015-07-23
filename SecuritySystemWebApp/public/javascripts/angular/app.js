var securitySystem = angular.module('securitySystem', ['ngRoute', 'ngMaterial', 'datePicker']);

securitySystem.config(['$routeProvider','$locationProvider',function($routeProvider, $locationProvider){


  $routeProvider
  .when('/',{
    templateUrl:'javascripts/angular/views/home.html'
  })

  $locationProvider.hashPrefix('!')
}]);