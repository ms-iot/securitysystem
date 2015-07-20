securitySystem.controller('homeCtrl', ['$scope', '$http', '$location', '$anchorScroll', '$mdSidenav', '$rootScope', function($scope, $http, $location, $anchorScroll, $mdSidenav, $rootScope){


  var staticImagesArray = [],
      days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
  $scope.positionToken = null;
  $scope.dayList = [[],[]];
  $scope.loading = true;
  $scope.imageUrl;
  $scope.selectedDate;


  $scope.getImages = function(){
    if($scope.positionToken === null && staticImagesArray.length === 0) $scope.loading = true;
    $http.post('/images', {url: $location.absUrl(), token: $scope.positionToken})
      .success(function(response){
        // staticImagesArray.reverse()
        for(var i = 0; i < response.images.length; i++){
           //DateTime formatting.
          var date = new Date(response.images[i].milliseconds),
              localDate = date.toLocaleString(),
              day = new Date(response.images[i].milliseconds).getDay(),
              dateObject = new Date(localDate.slice(0, localDate.search(','))),
              formattedDate = dateObject.toISOString().slice(0,10);
          response.images[i].date = localDate;
          response.images[i].day = days[day];
          staticImagesArray.push(response.images[i])
          if($scope.dayList[0].indexOf(formattedDate) === -1){
            $scope.dayList[0].push(formattedDate);
            $scope.dayList[1].push(days[dateObject.getDay()]);
          }
        }
        // staticImagesArray.reverse();
        if(response.token) $scope.positionToken = response.token;
        $scope.images = staticImagesArray;
        $scope.viewImage = $scope.images[0];
        $scope.switchImage($scope.viewImage);
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

  $scope.scrollSpy = function(index){
    var day = (new Date(index)).getDay();
    $location.hash(days[day]);
    $anchorScroll();
    $location.hash(null);
  }



}]);