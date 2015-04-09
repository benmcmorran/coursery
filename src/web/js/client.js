var primarySort = "rc";
var results = [];
var rowsDisplayed = 0;

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
    
    var years = [];
    var subjects = [];
    var courseNums = [];
    var sections = [];
    var instructors = [];
    
    for (var i = 0; i < courses.length; i++) {
        var course = courses[i];
        if ($.inArray(course.yr, years) == -1) { years.push(course.yr); }
        if ($.inArray(course.su, subjects) == -1) { subjects.push(course.su); }
        if ($.inArray(course.cr, courseNums) == -1) { courseNums.push(course.cr); }
        if ($.inArray(course.sc, sections) == -1) { sections.push(course.sc); }
        if ($.inArray(course.in, instructors) == -1) { instructors.push(course.in); }
    }
    
    years.sort(function(a, b) { return a < b; });
    subjects.sort(alphanum);
    courseNums.sort(alphanum);
    sections.sort(alphanum);
    instructors.sort(alphanum);
    
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
        if ($(window).scrollTop() >= $(document).height() - $(window).height() - 100) {
            var html = generateRows(100,
                $("#year").val() != "Average",
                $("#subject").val() != "Average",
                $("#course").val() != "Average",
                $("#section").val() != "Average",
                $("#instructor").val() != "Average");
            $("#results").append(html);
        }
    };

    setInterval(scrollListener, 500);
});

function fillSelect(element, values, transform) {
    element = element[0];
    var html = "";
    var createOption = function(name, value) {
        var opt = document.createElement('option');
        opt.innerHTML = name;
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
    var selectedYear = $("#year").val();
    var selectedSubject = $("#subject").val();
    var selectedCourseNum = $("#course").val();
    var selectedSection = $("#section").val();
    var selectedInstructor = $("#instructor").val();
    
    NProgress.start();
    var filtered = [];
    var i = 0;
    rowsDisplayed = 0;
    
    function filterFn(callback) {
        var end = Math.min(i + 100, courses.length);
        for (; i < end; i++) {
            if ((selectedYear == "All" || selectedYear == "Average" || parseInt(selectedYear) == courses[i].yr) &&
                (selectedSubject == "All" || selectedSubject == "Average" || selectedSubject == courses[i].su) &&
                (selectedCourseNum == "All" || selectedCourseNum == "Average" || selectedCourseNum == courses[i].cr) &&
                (selectedSection == "All" || selectedSection == "Average" || selectedSection == courses[i].sc) &&
                (selectedInstructor == "All" || selectedInstructor == "Average" || selectedInstructor == courses[i].in)) {
                filtered.push(courses[i]);
            }
        }
        
        if (end == courses.length) {
            i = 0;
            NProgress.set(.17);
            callback();
        } else {
            setTimeout(function() { filterFn(callback); }, 0);
        }
    }
    
    var groups = [];
    
    function groupFn(callback) {
        var end = Math.min(i + 100, filtered.length);
        for (; i < end; i++) {
            var inGroup = false;
            for (var j = 0; j < groups.length; j++) {
                if ((selectedYear != "All" || groups[j][0].yr == filtered[i].yr) &&
                    (selectedSubject != "All" || groups[j][0].su == filtered[i].su) &&
                    (selectedCourseNum != "All" || groups[j][0].cr == filtered[i].cr) &&
                    (selectedSection != "All" || groups[j][0].sc == filtered[i].sc) &&
                    (selectedInstructor != "All" || groups[j][0].in == filtered[i].in)) {
                    groups[j].push(filtered[i]);
                    inGroup = true;
                    break;
                }
            }
            if (!inGroup) {
                groups.push([filtered[i]]);
            }
        }
        
        if (end == filtered.length) {
            i = 0;
            NProgress.set(.84);
            callback();
        } else {
            setTimeout(function() { groupFn(callback); }, 0);
        }
    }
    
    results = [];
    
    function averageFn(callback) {
        var end = Math.min(i + 100, groups.length);
        for (; i < end; i++) {
            var result = averageGroup(groups[i]);
            results.push(result);
        }
        
        if (end == groups.length) {
            i = 0;
            NProgress.set(.92);
            callback();
        } else {
            setTimeout(function() { averageFn(callback); }, 0);
        }
    }
    
    function finalFn() {
        results.sort(function(a, b) {
            var multiplier = 1;
            if (primarySort == "rc" || primarySort == "ri" || primarySort == "wl") { multiplier = -1; }
            return multiplier * alphanum(a[primarySort].toString(), b[primarySort].toString());
        });
        
        var html = generateRows(100,
            selectedYear != "Average",
            selectedSubject != "Average",
            selectedCourseNum != "Average",
            selectedSection != "Average",
            selectedInstructor != "Average");
        
        $("#results").hide(200, function() { $(this).empty().append(html).show(200); });
        
        NProgress.done();
        callback();
    }
    
    filterFn(function() { groupFn(function() { averageFn(function() { finalFn(); }); }); });
}

function averageGroup(group) {
    var result = { yr: group[0].yr, su: group[0].su, cr: group[0].cr, sc: group[0].sc, in: group[0].in, rc: 0.0, ri: 0.0, wl: 0.0, gr: 0 };

    for (var i = 0; i < group.length; i++) {
        result.rc += group[i].rc;
        result.ri += group[i].ri;
        result.wl += group[i].wl;
        result.gr += group[i].gr == "A" ? 4 : group[i].gr == "B" ? 3 : group[i].gr == "C" ? 2 : 1;
    }
    
    result.rc = result.rc / group.length;
    result.ri = result.ri / group.length;
    result.wl = result.wl / group.length;
    result.gr = result.gr / group.length;
    result.gr = result.gr > 3.5 ? "A" : result.gr > 2.5 ? "B" : result.gr > 1.5 ? "C" : "NR";
    
    return result;
}

function generateRows(maxLength, showYear, showSubject, showCourse, showSection, showInstructor) {
    var html = "";
    var end = Math.min(rowsDisplayed + maxLength, results.length);
    for (var i = rowsDisplayed; i < end; i++) {
        html += generateRow(results[i], showYear, showSubject, showCourse, showSection, showInstructor);
    }
    rowsDisplayed = end;
    return html;
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

// Taken from http://my.opera.com/GreyWyvern/blog/show.dml/1671288
function alphanum(a, b) {
  function chunkify(t) {
    var tz = [], x = 0, y = -1, n = 0, i, j;

    while (i = (j = t.charAt(x++)).charCodeAt(0)) {
      var m = (i == 46 || (i >=48 && i <= 57));
      if (m !== n) {
        tz[++y] = "";
        n = m;
      }
      tz[y] += j;
    }
    return tz;
  }

  var aa = chunkify(a);
  var bb = chunkify(b);

  for (x = 0; aa[x] && bb[x]; x++) {
    if (aa[x] !== bb[x]) {
      var c = Number(aa[x]), d = Number(bb[x]);
      if (c == aa[x] && d == bb[x]) {
        return c - d;
      } else return (aa[x] > bb[x]) ? 1 : -1;
    }
  }
  return aa.length - bb.length;
}