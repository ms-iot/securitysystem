var bigInt = requre('big-integer');

var timeFormat = function(image) {
  // using npm module bigInt, because the number of .NET ticks
  // is a number with too many digits for vanilla JavaScript
  // to perform accurate math on.
  var ticks = image.name.slice(19,37);
              ticksAtUnixEpoch = bigInt("621355968000000000"),
              ticksInt = bigInt(ticks),
              ticksSinceUnixEpoch = ticksInt.minus(ticksAtUnixEpoch),
              milliseconds = ticksSinceUnixEpoch.divide(10000),
              date = new Date(milliseconds.value),
              day = days[date.getDay()],
              localDate = date.toLocaleString();

  image.date = localDate;
  image.day = day;
  image.hour = image.name.slice(16,18)
  return image
}