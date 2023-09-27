var tags = [];

var margin = { top: 10, right: 10, bottom: 10, left: 10 },
    width = 300 - margin.left - margin.right,
    height = 400 - margin.top - margin.bottom;

var svg = d3.select("#wordCloud").append("svg")
    .attr("width", width + margin.left + margin.right)
    .attr("height", height + margin.top + margin.bottom)
    .append("g")
    .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

var layout = d3.layout.cloud()
    .size([width, height])
    .words(tags.map(function (d) {
        return { text: d.text, size: d.size };
    }))
    .padding(10)
    .rotate(function () { return ~~(Math.random() * 2) * 90; })
    .fontSize(function (d) { return d.size; })
    .on("end", draw);

window.onload = async function () {
    tags = await getTagList();
    tags = tags.map(function (tag) {
        return {
            text: tag,
            size: getRandomSize(5, 40)
        };
    });
    layout.words(tags);
    layout.start();
};

function getRandomSize(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

async function getTagList() {
    let response = await fetch('/Tag/GetAllTags/', {
        method: 'GET',
    });
    return response.json();
}

function draw(words) {
    svg.append("g")
        .attr("transform", "translate(" + width / 2 + "," + (height / 2 - 20) + ")")
        .selectAll("text")
        .data(words)
        .enter().append("text")
        .style("font-size", function (d) { return d.size + "px"; })
        .style("fill", "#69b3a2")
        .attr("text-anchor", "middle")
        .style("font-family", "Impact")
        .attr("transform", function (d) {
            return "translate(" + [d.x, d.y] + ")rotate(" + d.rotate + ")";
        })
        .text(function (d) { return d.text; })
        .on("click", function (d) {
            var form = document.createElement("form");
            form.method = "get";
            form.action = "/Review/Reviews";
            var input = document.createElement("input");
            input.type = "hidden";
            input.name = "searchQuery";
            input.value = d.text;
            form.appendChild(input);
            document.body.appendChild(form);
            form.submit();
            document.body.removeChild(form);
        });
}
