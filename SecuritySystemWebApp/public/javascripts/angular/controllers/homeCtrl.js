securitySystem.controller('homeCtrl', ['$scope', '$http', '$location', '$mdSidenav', '$rootScope', '$mdDialog', function($scope, $http, $location, $mdSidenav, $rootScope, $mdDialog){


  var staticImagesArray = [];
  $scope.loading = true;
  $scope.searchDate;
  $scope.imageUrl;

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
    $http.post('/images', {url: $location.absUrl(), date: todaysDate})
      .success(function(response){
        staticImagesArray = response.images.reverse();
        $scope.images = staticImagesArray;
        $scope.viewImage = $scope.images[0];
        $scope.switchImage($scope.viewImage);
        $scope.loading = false;
    });
  };
  $scope.getImages();

  $scope.switchImage = function(image){
      $scope.imageUrl = image ? image.downloadUrl : null;
      $scope.viewImage = image ? image : null;
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

  $scope.openCalendar = function(event) {
    $mdDialog.show({
      templateUrl: 'javascripts/angular/views/calendar.html',
      scope: $scope,
      preserveScope: true,
      targetEvent: event,
      clickOutsideToClose: true,
      escapeToClose: true
    })
    .then(function(date) {
      if(date) {
        $scope.searchDate = date;
        $scope.getImages(date);
      }
    })
  }

  $scope.openTimeSelector = function(event) {
    $mdDialog.show({
      templateUrl:'javascripts/angular/views/timePicker.html',
      scope: $scope,
      preserveScope: true,
      targetEvent: event,
      clickOutsideToClose: true,
      escapeToClose: true
    })
    .then(function(time) {
      if(time) {
        var timeString = time.toString(),
            endPosition = timeString.search(':'),
            hour = timeString.slice(endPosition - 2, endPosition);
        $scope.images = [];
        for(var i = 0; i < staticImagesArray.length; i++){
          if(staticImagesArray[i].hour === hour){
            $scope.images.push(staticImagesArray[i]);
          }
        }
      }
    })

  }
  $scope.closeDialog = function(data, dialog) {
    if(data && dialog === "date") $mdDialog.hide(data);
    if(data && dialog === "time") $mdDialog.hide(data);
    $mdDialog.hide()
  }
  $scope.noPhotos = function() {
    if($scope.images && $scope.images.length === 0 && $scope.loading === false) return true
  }
  $scope.emptyImage = function() {
    if($scope.images) return staticImagesArray.length === 0 && $scope.images.length === 0;
  }
  $scope.isLoading = function() {
    return $scope.loading
  }

}]);