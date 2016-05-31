from flask import Flask, jsonify, request
app=Flask(__name__)

from coursery.database import get_courses, AVERAGE, ASCENDING, DESCENDING, get_options

@app.route('/api/options')
def options():
    response = jsonify(**get_options())
    response.headers['Access-Control-Allow-Origin'] = '*'
    return response

@app.route('/api/courses')
def courses():
    valid_args = { 'year', 'subject', 'course', 'section', 'instructor' }
    result = get_courses(
        order_by=request.args.get('order_by', 'course_rating'),
        order_direction=ASCENDING if request.args.get('order_direction', 'ascending') == 'ascending' else DESCENDING,
        limit=int(request.args.get('limit', 100)),
        offset=int(request.args.get('offset', 0)),
        **{
            key: AVERAGE if value == 'average' else value
            for key, value in request.args.items()
            if key in valid_args
        }
    )
    response = jsonify(result)
    response.headers['Access-Control-Allow-Origin'] = '*'
    return response
