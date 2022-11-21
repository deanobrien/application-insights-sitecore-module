window.onload = function () {
    $(".dailySwitch").click(function () {
        $("#daily").toggle();
        $("#hourly").toggle();
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