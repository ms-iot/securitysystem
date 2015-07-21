securitySystem.controller('homeCtrl', ['$scope', '$http', '$location', '$mdSidenav', '$rootScope', function($scope, $http, $location, $mdSidenav, $rootScope){


  var staticImagesArray = [],
      positionToken = null;
  $scope.dayList = [[],[]];
  $scope.loading = true;
  $scope.imageUrl;
  $scope.selectedDate;

  $scope.getImages = function(date){
    $scope.loading = true;
    var todaysDate = null,
        today = date ? date : new Date(),
        month = (today.getMonth() + 1).toString(),
        day = (today.getDate()).toString(),
        year = (today.getFullYear()).toString();
    if(day.length === 1) day = "0" + day;
    if(month.length === 1) month = "0" + month;
    todaysDate = month + "_" + day + "_" + year;
    $http.post('/images', {url: $location.absUrl(), token: positionToken, date: todaysDate})
      .success(function(response){
        staticImagesArray = response.images.reverse();
        if(response.token) positionToken = response.token;
        $scope.images = staticImagesArray;
        if($scope.images.length > 0) {
        $scope.viewImage = $scope.images[0];
        $scope.switchImage($scope.viewImage);
        }
        $scope.loading = false;
    });
  };
  $scope.getImages();

  $scope.switchImage = function(image){
    $scope.imageUrl = image['@content.downloadUrl'];
    $scope.viewImage = image;
  };

   $scope.imagePosition = function(){
    var first = true, last = true;
    if($scope.images && $scope.images.length !== 0){
      first = $scope.images.indexOf($scope.viewImage) == 0;
      last = $scope.images.indexOf($scope.viewImage) == $scope.images.length - 1;
    }
    return {first: first, last: last}
  };

  $scope.browsePicture = function(direction){
    var currentIndex = $scope.images.indexOf($scope.viewImage);
    $scope.viewImage = $scope.images[currentIndex + direction];
    $scope.switchImage($scope.viewImage);
  };

  $rootScope.toggleSidenav = function(id) {
      $mdSidenav(id).toggle();
   }

}]);