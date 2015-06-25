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
securitySystem.filter('btnFormatFilter', function(){
  return function(input){
    var strikeThroughArray = [];
    var btnClassArray = [];
    if(input.mon == true){
      strikeThroughArray.push('<s><span class="innerStrikeThrough">M<span></s>');
      btnClassArray.push('btn-default');
    } else {
      strikeThroughArray.push('M');
      btnClassArray.push('btn-success');
    }
    if(input.tue == true){
      strikeThroughArray.push('<s><span class="innerStrikeThrough">T<span></s>');
      btnClassArray.push('btn-default');
    } else {
      strikeThroughArray.push('T');
      btnClassArray.push('btn-success');
    }
    if(input.wed == true){
      strikeThroughArray.push('<s><span class="innerStrikeThrough">W<span></s>');
      btnClassArray.push('btn-default');
    } else {
      strikeThroughArray.push('W');
      btnClassArray.push('btn-success');
    }
    if(input.thu == true){
      strikeThroughArray.push('<s><span class="innerStrikeThrough">Th<span></s>');
      btnClassArray.push('btn-default');
    } else {
      strikeThroughArray.push('Th');
      btnClassArray.push('btn-success');
    }
    if(input.fri == true){
      strikeThroughArray.push('<s><span class="innerStrikeThrough">F<span></s>');
      btnClassArray.push('btn-default');
    } else {
      strikeThroughArray.push('F');
      btnClassArray.push('btn-success');
    }
    if(input.sat == true){
      strikeThroughArray.push('<s><span class="innerStrikeThrough">Sa<span></s>');
      btnClassArray.push('btn-default');
    } else {
      strikeThroughArray.push('Sa');
      btnClassArray.push('btn-success');
    }
    if(input.sun == true){
      strikeThroughArray.push('<s><span class="innerStrikeThrough">S<span></s>');
      btnClassArray.push('btn-default');
    } else {
      strikeThroughArray.push('S');
      btnClassArray.push('btn-success');
    }
    var btnFormat = {
      strikeThrough: strikeThroughArray,
      btnClass: btnClassArray
    }
    return btnFormat;
  }
})
