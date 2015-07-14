
securitySystem.controller('homeCtrl', ['$scope', '$http', '$location', 'dayFilterFilter', function($scope, $http, $location , dayFilterFilter){


var staticImagesArray = [];
var days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
$scope.checkModel = {
    mon: false,
    tue: false,
    wed: false,
    thu: false,
    fri: false,
    sat: false,
    sun: false
  };
$scope.loading = true;

  $scope.getImages = function(){
    $scope.loading = true;

    $http.get('/images')
      .success(function(blobData){
        console.log(blobData);
        for(var i = 0; i < blobData.entries.length; i++){
          //DateTime formatting.
          var date = new Date(blobData.entries[i].milliseconds).toLocaleString();
          blobData.entries[i].date = date;
          switch(new Date(date).getDay()){
            case 1:
              blobData.entries[i].day = days[1];
              break;
            case 2:
              blobData.entries[i].day = days[2];
              break;
            case 3:
              blobData.entries[i].day = days[3];
              break;
            case 4:
              blobData.entries[i].day = days[4];
              break;
            case 5:
              blobData.entries[i].day = days[5];
              break;
            case 6:
              blobData.entries[i].day = days[6];
              break;
            case 7:
              blobData.entries[i].day = days[0];
              break;
          }
        }
    staticImagesArray = blobData.entries;
    $scope.images = dayFilterFilter(staticImagesArray, $scope.checkModel)
    $scope.viewImage = $scope.images[$scope.images.length - 1];
    $scope.switchImage($scope.viewImage)
    $scope.loading = false;
  })
  }

  $scope.getImages();
  $scope.imageUrl;
  $scope.switchImage = function(image, redirect){
      $http.get('/image/'+image.name)
        .success(function(url){
          console.log('made it : ', url)
          $scope.imageUrl = url
          $scope.viewImage = image;
          if(redirect === true){
            $location.path('/')
          }
        })
  }


  $scope.browsePicture = function(direction){

    var currentIndex =  $scope.images.indexOf($scope.viewImage);
    if(direction == 'left' && $scope.images.indexOf($scope.viewImage) != 0){
      $scope.viewImage = $scope.images[currentIndex - 1];
      $scope.switchImage($scope.viewImage)
    } else if(direction == 'left'){
      return;
    }else if($scope.images.indexOf($scope.viewImage) != $scope.images.length - 1) {
      $scope.viewImage = $scope.images[currentIndex + 1];
      $scope.switchImage($scope.viewImage)
    }
  }

  $scope.$watchCollection('checkModel', function(newValue, oldValue){
      $scope.images = dayFilterFilter(staticImagesArray, $scope.checkModel)
      $scope.viewImage = $scope.images[$scope.images.length - 1];
  })




}])