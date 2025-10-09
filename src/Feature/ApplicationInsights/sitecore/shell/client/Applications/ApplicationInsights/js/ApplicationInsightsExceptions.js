loadExceptionCluster(id, problemIdBase64, innerMessageBase64, timespan);
function loadExceptionCluster(id, problemIdBase64, innerMessageBase64, timespan) {
    var exceptionCluster = $("#groupedExceptions");
    var alertsCluster = $("#triggeredAlerts");
    var loader = $("#groupedExceptions .loader");
    var noResult = $("#groupedExceptions .noResult");
    var alertLoader = $("#triggeredAlerts .loader");
    var noAlerts = $("#triggeredAlerts .noResult");
    loader.show();
    $.getJSON("/sitecore/shell/sitecore/client/applications/applicationinsights/getalerts/" + id + "?timespan=" + timespan, function (alertData) {
        console.log("Calling: getalerts/" + id + "?timespan=" + timespan);
        if (alertData.ErrorMessage != null) {
            noAlerts.show();
            noAlerts.append("<p>" + alertData.ErrorMessage + "</p>");
        } else if (alertData.length >= 1) {
            $.each(alertData, function (index, elem) {
                var template = $('#triggeredAlertTpl').html();
                var html = Mustache.to_html(template, elem);
                alertsCluster.append(html);
            });
        }
        else {
            alertsCluster.hide();
        }
        alertLoader.hide();
    }).then(function () {
        console.log("Calling: groupedexceptions/" + id + "?problemIdBase64=" + problemIdBase64 + "&InnerMostMessageBase64=" + innerMessageBase64 + "&timespan=" + timespan);
        $.getJSON("/sitecore/shell/sitecore/client/applications/applicationinsights/groupedexceptions/" + id + "?problemIdBase64=" + problemIdBase64 + "&InnerMostMessageBase64=" + innerMessageBase64 + "&timespan=" + timespan, function (exceptionData) {
            if (exceptionData.ErrorMessage != null) {
                noResult.show();
                noResult.append("<p>" + exceptionData.ErrorMessage + "</p>");
            } else if (exceptionData.length == 1 && exceptionData[0].InnerMostMessage != null && exceptionData[0].InnerMostMessage.length != 0) {
                $.each(exceptionData, function (index, elem) {
                    var template = $('#singleGroupedExceptionTpl').html();
                    var html = Mustache.to_html(template, elem);
                    exceptionCluster.append(html);
                });
            } else if (exceptionData.length > 1 && exceptionData[0].InnerMostMessage != null && exceptionData[0].InnerMostMessage.length != 0) {
                $.each(exceptionData, function (index, elem) {
                    var template = $('#groupedExceptionWithMessageTpl').html();
                    var html = Mustache.to_html(template, elem);
                    exceptionCluster.append(html);
                });
            } else if (exceptionData.length == 0) {
                noResult.show();
            }
            else {
                $.each(exceptionData, function (index, elem) {
                    var template = $('#groupedExceptionTpl').html();
                    var html = Mustache.to_html(template, elem);
                    exceptionCluster.append(html);
                });
            }
            loader.hide();
        });
    }).then(function () {
        CallOverviewAfterPageLoad();
    }).always(function () {
        console.log("Json calls complete");
    });
}
