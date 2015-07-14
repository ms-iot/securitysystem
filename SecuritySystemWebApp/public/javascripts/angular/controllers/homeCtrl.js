securitySystem.controller('homeCtrl', ['$scope', '$http', '$location', '$anchorScroll', '$mdSidenav', '$rootScope', function($scope, $http, $location, $anchorScroll, $mdSidenav, $rootScope){


  var staticImagesArray = [],
      days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],
      storageService;
  $scope.dateList = []
  $scope.dayList = []
  $scope.loading = true;
  $scope.imageUrl;
  $scope.selectedDate;

  $scope.getImages = function(){
    $scope.loading = true;
    $http.post('/images', {url: $location.absUrl()})
      .success(function(response){
        storageService = response.storageService
        for(var i = 0; i < response.images.length; i++){
          //DateTime formatting.
          var date = new Date(response.images[i].milliseconds)
          var localDate = date.toLocaleString();
          response.images[i].date = localDate;
          var day = new Date(response.images[i].milliseconds).getDay();
          response.images[i].day = days[day]
          var dateOnly = localDate.slice(0, localDate.search(','))
          if($scope.dateList.indexOf((new Date(dateOnly)).toISOString().slice(0,10)) === -1){
            $scope.dateList.push((new Date(dateOnly)).toISOString().slice(0,10))
            $scope.dayList.push(days[(new Date(dateOnly)).getDay()])
          }
        }
        staticImagesArray = response.images;
        staticImagesArray = staticImagesArray.reverse()
        $scope.images = staticImagesArray;
        $scope.viewImage = $scope.images[0];
        $scope.switchImage($scope.viewImage);
        $scope.loading = false;
    });
  };
  $scope.getImages();


  $scope.switchImage = function(image, redirect){
    if(storageService === "azure") {
      $http.get('/image/'+image.name)
        .success(function(url){
          $scope.imageUrl = url
          $scope.viewImage = image;
        })
    } else {
      $scope.imageUrl = image["@content.downloadUrl"]
    }
  };

   $scope.imagePosition = function(){
    var first = true, last = true;

    if($scope.images.length !== 0){
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
      $mdSidenav(id).toggle()
   }

  $scope.scrollSpy = function(index){
    var day = (new Date(index)).getDay()
    $location.hash(days[day])
    $anchorScroll()
    $location.hash('')
  }

}]);