var primarySort = "rc";
var results = [];
var rowsDisplayed = 0;
var apiRoot = 'http://localhost:5000/api';
var isAtEnd = false;

$(function() {
    $("[rel='tooltip']").tooltip();
    
    var fields = ["yr", "su", "cr", "sc", "in", "rc", "ri", "wl", "gr"];
    for (var i = 0; i < fields.length; i++) {
        $("#" + fields[i] + "Header").click((function(field) {
            return function(e) {
                primarySort = field;
                
                populateTable(function() { $(e.target).append($("#sortArrow")); });
            }
        })(fields[i]));
    }
    
    $.getJSON(apiRoot + '/options', function (data) {
        var years = data.year,
            subjects = data.subject,
            courseNums = data.course,
            sections = data.section,
            instructors = data.instructor; 
        
        fillSelect($("#year"), years, function(year) { return (year - 1) + "-" + year; });
        fillSelect($("#subject"), subjects);
        fillSelect($("#course"), courseNums);
        fillSelect($("#section"), sections);
        fillSelect($("#instructor"), instructors, function(instructor) {
            if (instructor.length > 20) {
                return instructor.substring(0, 17) + "...";
            } else {
                return instructor;
            }});
            
        $("#year").change(populateTable);
        $("#subject").change(populateTable);
        $("#course").change(populateTable);
        $("#section").change(populateTable);
        $("#instructor").change(populateTable);
        
        populateTable();
        
        // Taken from http://stackoverflow.com/questions/4841585/alternatives-to-jquery-endless-scrolling
        var scrollListener = function () {
            if (!isAtEnd && $(window).scrollTop() >= $(document).height() - $(window).height() - 100) {
                generateRows(100,
                    $("#year").val() != "Average",
                    $("#subject").val() != "Average",
                    $("#course").val() != "Average",
                    $("#section").val() != "Average",
                    $("#instructor").val() != "Average",
                    function (html) {
                        $("#results").append(html);
                    });
            }
        };

        setInterval(scrollListener, 500);
    });
});

function fillSelect(element, values, transform) {
    element = element[0];
    var html = "";
    var createOption = function(name, value) {
        var opt = document.createElement('option');
        opt.textContent = name;
        var val = value === undefined ? name : value;
        opt.value = val;
        return opt;
    }
    var all = createOption("All");
    var avg = createOption("Average");
    // debugger;
    element.appendChild(all);
    element.appendChild(avg);
    if (transform == null) { transform = function(val) { return val; } }
    for (var i = 0; i < values.length; i++) {
        var opt = createOption(transform(values[i]), values[i]);
        element.appendChild(opt);
    }
}

function populateTable(callback) {
    rowsDisplayed = 0;
    results = [];
    isAtEnd = false;
    generateRows(100,
        $("#year").val() != "Average",
        $("#subject").val() != "Average",
        $("#course").val() != "Average",
        $("#section").val() != "Average",
        $("#instructor").val() != "Average",
        function (html) {
            $("#results").hide(200, function() { $(this).empty().append(html).show(200); });
            if (typeof callback === 'function') callback();
        });
}


function getCourseData(callback) {
    var selectedYear = $("#year").val();
    var selectedSubject = $("#subject").val();
    var selectedCourseNum = $("#course").val();
    var selectedSection = $("#section").val();
    var selectedInstructor = $("#instructor").val();
    
    NProgress.start();
    
    var data = {};
    function addQueryElement(name, value) {
        if (value == 'All') return;
        if (value == 'Average') data[name] = 'average';
        else data[name] = value;
    }

    addQueryElement('year', selectedYear);
    addQueryElement('subject', selectedSubject);
    addQueryElement('course', selectedCourseNum);
    addQueryElement('section', selectedSection);
    addQueryElement('instructor', selectedInstructor);

    var fieldMapping = {
        yr: 'year',
        su: 'subject',
        cr: 'course',
        sc: 'section',
        in: 'instructor',
        rc: 'course_rating',
        ri: 'instructor_rating',
        wl: 'workload',
        gr: 'grade'
    };

    var reverseMapping = {
        year: 'yr',
        subject: 'su',
        course: 'cr',
        section: 'sc',
        instructor: 'in',
        course_rating: 'rc',
        instructor_rating: 'ri',
        workload: 'wl',
        grade: 'gr'
    };

    data['order_by'] = fieldMapping[primarySort];
    data['order_direction'] = ['rc', 'ri', 'wl', 'gr'].indexOf(primarySort) != -1 ? 'descending' : 'ascending';
    data['offset'] = rowsDisplayed;

    $.getJSON(apiRoot + '/courses', data, function (courseResult) {
        if (courseResult.length == 0) isAtEnd = true;
        for (var i = 0; i < courseResult.length; i++) {
            newResult = {};
            for (key in courseResult[i]) {
                newResult[reverseMapping[key]] = courseResult[i][key];
            }
            results.push(newResult);
        }
        
        NProgress.done();
        callback();
    });
}

function generateRows(maxLength, showYear, showSubject, showCourse, showSection, showInstructor, callback) {
    var html = "";
    getCourseData(function () {
        var end = Math.min(rowsDisplayed + maxLength, results.length);
        for (var i = rowsDisplayed; i < end; i++) {
            html += generateRow(results[i], showYear, showSubject, showCourse, showSection, showInstructor);
        }
        rowsDisplayed = end;
        callback(html);
    });
}

function generateRow(course, showYear, showSubject, showCourse, showSection, showInstructor) {
    crClass = course.rc >= 4 ? "good" : course.rc >= 3 ? "okay" : "bad";
    inClass = course.ri >= 4 ? "good" : course.ri >= 3 ? "okay" : "bad";
    wlClass = course.wl <= 15 ? "good" : course.wl <= 20 ? "okay" : "bad";
    grClass = course.gr == "A" ? "good" : course.gr == "B" ? "okay" : "bad";

    return "<tr><td>" + (showYear ? (course.yr - 1) + "-" + course.yr : "") + "</td>" +
           "<td>" + (showSubject ? course.su : "") + "</td>" +
           "<td>" + (showCourse ? course.cr : "") + "</td>" + 
           "<td>" + (showSection ? course.sc : "") + "</td>" +
           "<td>" + (showInstructor ? course.in : "") + "</td>" +
           "<td class=\"" + crClass + "\">" + course.rc.toFixed(1) + "</td>" +
           "<td class=\"" + inClass + "\">" + course.ri.toFixed(1) + "</td>" +
           "<td class=\"" + wlClass + "\">" + course.wl.toFixed(0) + "</td>" +
           "<td class=\"" + grClass + "\">" + course.gr + "</td>" +
           ((showYear && showSubject && showCourse && showSection) ?
           "<td><a target=\"_blank\" title=\"View full report\" href=\"evaluations/" + course.yr + "-" + course.su + "-" + course.cr + "-" + course.sc + ".html\"><span class=\"glyphicon glyphicon-new-window\"></span></a></td>" :
           "<td></td>");
}
