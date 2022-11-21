loadExceptionCluster(id, problemIdBase64, innerMessageBase64, timespan);
function loadExceptionCluster(id, problemIdBase64, innerMessageBase64, timespan) {
    var exceptionCluster = $("#groupedExceptions");
    var loader = $("#groupedExceptions .loader");
    var noResult = $("#groupedExceptions .noResult");
    loader.show();
    $.getJSON("/sitecore/shell/sitecore/client/applications/applicationinsights/groupedexceptions/" + id + "?problemIdBase64=" + problemIdBase64 + "&InnerMostMessageBase64=" + innerMessageBase64 + "&timespan=" + timespan, function (data) {
        if (data.ErrorMessage != null) {
            noResult.show();
            noResult.append("<p>" + data.ErrorMessage+"</p>");
        }else if (data.length == 1 && data[0].InnerMostMessage != null && data[0].InnerMostMessage.length != 0) {
            $.each(data, function (index, elem) {
                var template = $('#singleGroupedExceptionTpl').html();
                var html = Mustache.to_html(template, elem);
                exceptionCluster.append(html);
            });
        } else if (data.length > 1 && data[0].InnerMostMessage != null && data[0].InnerMostMessage.length != 0) {
            $.each(data, function (index, elem) {
                var template = $('#groupedExceptionWithMessageTpl').html();
                var html = Mustache.to_html(template, elem);
                exceptionCluster.append(html);
            });
        } else if (data.length == 0) {
            noResult.show();
        }
        else {
            $.each(data, function (index, elem) {
                var template = $('#groupedExceptionTpl').html();
                var html = Mustache.to_html(template, elem);
                exceptionCluster.append(html);
            });
        }
        loader.hide();
    });
}
