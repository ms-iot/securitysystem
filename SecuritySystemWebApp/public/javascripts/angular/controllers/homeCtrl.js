securitySystem.controller('homeCtrl', ['$scope', '$http', '$location', '$anchorScroll', 'dayFilter', function($scope, $http, $location, $anchorScroll, dayFilter){


  var staticImagesArray = [];
  var days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
  $scope.dayList = ["M", "T", "W", "Th", "F", "Sa", "S"]
  $scope.checkFilterModel = [false, false, false, false, false, false, false];
  $scope.loading = true;
  $scope.imageUrl;

  $scope.getImages = function(){
    $scope.loading = true;
    $http.get('/images')
      .success(function(blobData){
        for(var i = 0; i < blobData.entries.length; i++){
          //DateTime formatting.
          var date = new Date(blobData.entries[i].milliseconds).toLocaleString();
          blobData.entries[i].date = date;
          var day = new Date(blobData.entries[i].milliseconds).getDay();
          console.log(date);
          blobData.entries[i].day = days[day - 1]
        }
    staticImagesArray = blobData.entries;
    $scope.images = dayFilter(staticImagesArray, $scope.checkFilterModel)
    $scope.viewImage = $scope.images[$scope.images.length - 1];
    $scope.switchImage($scope.viewImage)
    $scope.loading = false;
    });
  };

  $scope.getImages();


  $scope.switchImage = function(image, redirect){
      $http.get('/image/'+image.name)
        .success(function(url){
          $scope.imageUrl = url
          $scope.viewImage = image;
          if(redirect === true){
            $location.path('/')
          }
        })
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
    if(direction == 'left'){
      $scope.viewImage = $scope.images[currentIndex - 1];
      $scope.switchImage($scope.viewImage)
    } else {
      $scope.viewImage = $scope.images[currentIndex + 1];
      $scope.switchImage($scope.viewImage)
    }
  };

  $scope.scrollSpy = function(index){
    $anchorScroll(days[index])
  }

  $scope.$watchCollection('checkFilterModel', function(newValue, oldValue){
      $scope.images = dayFilter(staticImagesArray, $scope.checkFilterModel, days)
      if($scope.images[$scope.images.length - 1]) {
        $scope.switchImage($scope.images[$scope.images.length - 1])
      };
  });

}]);