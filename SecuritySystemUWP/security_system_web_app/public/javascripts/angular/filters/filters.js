securitySystem.filter('motionFilter', function(){

  return function(input, filterOn){
    if(!input)  return input

    var filteredArray = [];
    if(filterOn === true){
       for(var i = 0; i < input.length; i++){
          if(input[i].name.charAt(19) == '1'){
            filteredArray.push(input[i])
          }
        }
        return filteredArray;
    }

    return input
  }
});

securitySystem.filter('dayFilter', function(){

  return function(input, checkModel){
    var filteredArray = [];
    for(var i = 0; i < input.length; i++){
      switch(input[i].day){
        case "Monday":
          if(checkModel.mon === false){
            filteredArray.push(input[i])
          }
          break;
        case "Tuesday":
          if(checkModel.tue === false){
            filteredArray.push(input[i])
          }
          break;
        case "Wednesday":
          if(checkModel.wed === false){
            filteredArray.push(input[i])
          }
          break;
        case "Thursday":
          if(checkModel.thu === false){
            filteredArray.push(input[i])
          }
          break;
        case "Friday":
          if(checkModel.fri === false){
            filteredArray.push(input[i])
          }
          break;
        case "Saturday":
          if(checkModel.sat === false){
            filteredArray.push(input[i])
          }
          break;
        case "Sunday":
          if(checkModel.sun === false){
            filteredArray.push(input[i])
          }
          break;
      }
    }
    return filteredArray
  }



})