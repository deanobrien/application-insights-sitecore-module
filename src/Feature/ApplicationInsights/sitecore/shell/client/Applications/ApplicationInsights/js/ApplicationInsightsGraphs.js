window.onload = function () {
    $(".dailySwitch").click(function () {
        $("#daily").toggle();
        $("#hourly").toggle();
    });

    $(".variationToggle").click(function () {
        $(".variation").hide();
        $("#variation" + $(this).attr("var-id")).show();
        $(".variationToggle").removeClass("btn-primary");
        $(this).addClass("btn-primary");
        var file = $(this).attr("var-file");
        var sysPrompt = $(this).attr("var-sysPrompt");
        var id = $(this).attr("var-id");
        getAIOverview(sysPrompt, file, id);
    });


    var lineChartData = {
        labels: dailyLabels,
        datasets: [
            {

                label: "Applications",
                fillColor: "rgba(151,187,205,0.2)",
                strokeColor: "rgba(151,187,205,1)",
                pointColor: "rgba(151,187,205,1)",
                pointStrokeColor: "#fff",
                pointHighlightFill: "#fff",
                pointHighlightStroke: "rgba(151,187,205,1)",
                data: dailyValues
            }
        ]
    }
    var lineChartData2 = {
        labels: hourlyLabels,
        datasets: [
            {

                label: "Applications",
                fillColor: "rgba(151,187,205,0.2)",
                strokeColor: "rgba(151,187,205,1)",
                pointColor: "rgba(151,187,205,1)",
                pointStrokeColor: "#fff",
                pointHighlightFill: "#fff",
                pointHighlightStroke: "rgba(151,187,205,1)",
                data: hourlyValues
            }
        ]

    }
    var ctx = document.getElementById("canvas").getContext("2d");
    window.myLine1 = new Chart(ctx).Line(lineChartData, {
        responsive: true,
        legendTemplate: "<ul class=\"<%=name.toLowerCase()%>-legend\"><% for (var i=0; i<datasets.length; i++){%><li><span style=\"width:20px;height:20px;background-color:<%=datasets[i].fillColor%>\"> &nbsp; &nbsp; &nbsp; &nbsp;</span> <%if(datasets[i].label){%><%=datasets[i].label%><%}%></li><%}%></ul>"
    });
    var ctx2 = document.getElementById("canvas2").getContext("2d");
    window.myLine2 = new Chart(ctx2).Line(lineChartData2, {
        responsive: true,
        legendTemplate: "<ul class=\"<%=name.toLowerCase()%>-legend\"><% for (var i=0; i<datasets.length; i++){%><li><span style=\"width:20px;height:20px;background-color:<%=datasets[i].fillColor%>\"> &nbsp; &nbsp; &nbsp; &nbsp;</span> <%if(datasets[i].label){%><%=datasets[i].label%><%}%></li><%}%></ul>"
    });
    $("#hourly").hide();
};
function getAIOverview(sysPrompt, file, id) {
    if (allowAiSummary) {
        var aiOverview = $("#aiOverview" + id);
        var base64AiOverview = $("#base64AiOverview" + id);
        var fileName = $("#fileName" + id);
        var base64AiOverviewPR = $("#base64AiOverviewPR" + id);
        var fileNamePR = $("#fileNamePR" + id);

        $.post("/sitecore/shell/sitecore/client/applications/applicationinsights/GetAIOverview/"+id, { systemPrompt: sysPrompt, fileName: file }, function (data) {
            aiOverview.html(data.responseFromAI + "<br/><small>AI Overview can be disabled in settings.</small>");
            base64AiOverview.val(data.base64ResponseFromAI);
            fileName.val(data.file);
            base64AiOverviewPR.val(data.base64ResponseFromAI);
            fileNamePR.val(data.file);
        });
    }
}
function getAIHealthCheck(id, t) {
    var aiHealth = $("#aiHealth");

    if (allowAiHealthCheck) {
        console.log("Calling: GetHealthCheck/" + id + "&timespan=" + timespan);

        $.post("/sitecore/shell/sitecore/client/applications/applicationinsights/GetHealthCheck/" + id, { timespan: t }, function (healthData) {
            if (healthData.includes("APPLICATIONHEALTHY")) {
                var healthyHtml = "<div class='alert alert-success alert-dismissible' role='alert' style='margin-top:30px;width:200px;height:auto;float:right'><button type='button' class='close' data-dismiss='alert' aria-label='Close'><span aria-hidden='true'>&times;</span></button><strong>Good News!</strong> Nothing unusual detected in the last 24 hrs.<br/><br/><small>Health Check can be disabled in settings.</small></div>";
                aiHealth.html(healthyHtml);
            } else {
                var unhealthyHtml = "<div class='alert alert-danger alert-dismissible' role='alert' style='margin-top:30px;width:100%;height:auto;float:right'><button type='button' class='close' data-dismiss='alert' aria-label='Close'><span aria-hidden='true'>&times;</span></button><strong>Warning AI Health Check Failed!</strong> Application appears to be unhealthy." + healthData + "<br/><small>Health Check can be disabled in settings.</small></div>";
                aiHealth.html(unhealthyHtml);
            }
        });
    }
}
function getAISummary(id, t, add) {
    var aiOverview = $("#aiOverview");
    if (allowAiSummary) {
        console.log("Calling: GetAISummary/" + id + "&timespan=" + t);

        var askAiHtml = `
                    <div id="askAi">
                        <form class="navbar-form" role="search" style="float: right!important; margin: 10px" onsubmit="formSubmitted('`+ id + `','` + t +`');return false;">
                            <div class="form-group" >
                                <input type="text" id="promptDetails" name="promptDetails" class="form-control" placeholder="Ask AI about this data" style="min-width:200px">
                                <input type="hidden" id="id" name="id" value="`+ id +`" />
                                <input type="hidden" id="timespan" name="prompt" value="`+t+`"/>
                            </div>
                            <button type="submit" class="btn btn-primary">Submit</button>
                        </form>
                    </div>
                    `;
        $.post("/sitecore/shell/sitecore/client/applications/applicationinsights/GetAISummary/" + id, { timespan: t, additional: add }, function (overviewData) {
            aiOverview.html(askAiHtml + overviewData + "<br/><small>AI Summary can be disabled in settings.</small>");
        }).then(function () {
            getAIHealthCheck(id, t);
        });
    }
    else {
        getAIHealthCheck(id, t);
    }
}