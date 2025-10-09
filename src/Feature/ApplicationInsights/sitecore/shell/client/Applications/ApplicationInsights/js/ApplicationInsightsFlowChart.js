jsPlumb.ready(function () {
    loadFlowchart(timespan);
    jsPlumb.setContainer("jsplumb-container");
    var common = {
        connector: ["StateMachine"],
        anchor: ["Left", "Right"],
        endpoint: "Dot",
        isSource: true,
        isTarget: true,
    };
    function loadFlowchart(timespan) {
        $.getJSON("/sitecore/shell/sitecore/client/applications/applicationinsights/dependencies", function (data) {
            var common = {
                connector: ["StateMachine"],
                anchor: ["Top", "Bottom"],
                endpoint: ["Dot", { radius: 5 }],
                isSource: true,
                isTarget: true,
            };

            var edges = data.edges;
            $.each(edges, function (index, elem) {

                var x = (Math.random() / 2) + 0.5;
                jsPlumb.connect({
                    source: elem.source,
                    target: elem.target,
                    scope: "someScope",
                    overlays: [["Arrow", { width: 10, length: 10, location: 0.85 }], ["Label", { label: elem.label, location: x }]]
                }, common);
            });
            var nodes = data.nodes;
            $.each(nodes, function (index, elem) {
                jsPlumb.draggable($("#" + elem.id));
                checkExceptionHealth(elem.id, timespan);  // <== Uncommment if you want health to be determined by exceptions being present
                //checkAlertHealth(elem.id, timespan);    // <== Uncommment if you want health to be determined by generated alert being present
                addToDropdown(elem.id, elem.title);
            });
        }).then(function () {
            CallSummaryAfterPageLoad();
        });
    }
    function checkExceptionHealth(id, timespan) {
        $.getJSON("/sitecore/shell/sitecore/client/applications/applicationinsights/groupedexceptions/" + id + "?timespan=" + timespan, function (data) {
            if (data.ErrorMessage != null) {
                $("#" + id).addClass('warning');
            } else if (data.length > 0) {
                $("#" + id).addClass('error');
            } else {
                $("#" + id).addClass('good');
            }
        });
    }

    function checkAlertHealth(id, timespan) {
        $.getJSON("/sitecore/shell/sitecore/client/applications/applicationinsights/getalerts/" + id + "?timespan=" + timespan, function (data) {
            if (data.ErrorMessage != null) {
                $("#" + id).addClass('warning');
            } else if (data.length > 0) {
                $("#" + id).addClass('error');
            } else {
                $("#" + id).addClass('good');
            }
        });
    }

    function addToDropdown(id, title) {
        var html = "<option value=" + id + ">" + title + "</option>"
        $("#appDropdown").append(html);
    }
    $("#appDropdown").on('change', function () {
        location.href = this.value + "?timespan=" + timespan;
    });
});