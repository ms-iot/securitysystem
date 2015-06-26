securitySystem.filter('day', function(){

  return function(input, checkModel, days){
    if(!days) return input;
    var filteredArray = [];
    input.map(function(picture, i){
      if(checkModel[days.indexOf(picture.day)] === false) {
        filteredArray.push(picture);
      }
    });
    return filteredArray
  }
});