securitySystem.directive('infinitescroll', function () {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            var raw = element[0];
            element.bind('scroll', function () {
                if (raw.scrollTop + raw.offsetHeight >= raw.scrollHeight - 1) {
                    scope.$apply(attrs.infinitescroll);
                }
            });
        }
    };
});