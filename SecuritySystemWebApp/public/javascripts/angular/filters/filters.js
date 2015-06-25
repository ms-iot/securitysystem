securitySystem.filter('dayFilter', function(){

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
})
securitySystem.filter('btnFormatFilter', function(){
  return function(input){
    var strikeThroughArray = [];
    var btnClassArray = [];
    var dayMap = ["M", "T", "W", "Th", "F", "Sa", "S"];
    input.map(function(day, i){
      if(input[i] == true) {
        strikeThroughArray.push('<s><span class="innerStrikeThrough">' + dayMap[i] + '<span></s>');
        btnClassArray.push('btn-default');
      } else {
        strikeThroughArray.push(dayMap[i]);
        btnClassArray.push('btn-success');
      }
    })
    var btnFormat = {
      strikeThrough: strikeThroughArray,
      btnClass: btnClassArray
    }
    return btnFormat;
  }
})
